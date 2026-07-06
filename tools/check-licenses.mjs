import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { homedir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

export function parseNpmLockDependencies(lock) {
  if (!lock?.packages || typeof lock.packages !== 'object') {
    throw new Error('package-lock.json is missing the packages object.');
  }

  return Object.entries(lock.packages)
    .filter(([packagePath]) => packagePath.startsWith('node_modules/'))
    .map(([packagePath, metadata]) => {
      const name = packagePath.replace(/^.*node_modules\//, '');

      return {
        ecosystem: 'npm',
        name,
        version: metadata.version ?? 'UNKNOWN',
        license: normalizeLicense(metadata.license),
        source: packagePath
      };
    })
    .sort(compareDependencies);
}

export function parseNugetLicenseFromNuspec(nuspecText) {
  const licenseMatch = nuspecText.match(/<license\b[^>]*>([^<]+)<\/license>/i);
  if (licenseMatch) {
    return normalizeLicense(licenseMatch[1]);
  }

  const licenseUrlMatch = nuspecText.match(/<licenseUrl>([^<]+)<\/licenseUrl>/i);
  if (licenseUrlMatch) {
    return `licenseUrl:${licenseUrlMatch[1].trim()}`;
  }

  return 'MISSING';
}

export function classifyDependency(dependency, policy) {
  const license = normalizeLicense(dependency.license);
  const review = findReview(dependency, license, policy);
  const effectiveLicense = review?.approvedAs ?? license;

  if (!license || license === 'MISSING' || license === 'UNKNOWN') {
    if (review) {
      return { ...dependency, license, effectiveLicense, status: 'reviewed', review, issue: null };
    }

    return {
      ...dependency,
      license,
      effectiveLicense,
      status: 'missing_license',
      review: null,
      issue: `${dependency.ecosystem}:${dependency.name}@${dependency.version} is missing license metadata.`
    };
  }

  if (matchesForbiddenLicense(license, policy.forbiddenLicensePatterns ?? [])) {
    return {
      ...dependency,
      license,
      effectiveLicense,
      status: 'forbidden',
      review,
      issue: `${dependency.ecosystem}:${dependency.name}@${dependency.version} uses forbidden license ${license}.`
    };
  }

  if (matchesLicenseExpression(license, policy.allowedLicenseExpressions ?? [])) {
    return { ...dependency, license, effectiveLicense, status: 'allowed', review: null, issue: null };
  }

  if (matchesLicenseExpression(license, policy.conditionalLicenseExpressions ?? [])) {
    if (review) {
      return { ...dependency, license, effectiveLicense, status: 'reviewed', review, issue: null };
    }

    return {
      ...dependency,
      license,
      effectiveLicense,
      status: 'needs_review',
      review: null,
      issue: `${dependency.ecosystem}:${dependency.name}@${dependency.version} license ${license} requires review.`
    };
  }

  if (review) {
    return { ...dependency, license, effectiveLicense, status: 'reviewed', review, issue: null };
  }

  return {
    ...dependency,
    license,
    effectiveLicense,
    status: 'unknown_license',
    review: null,
    issue: `${dependency.ecosystem}:${dependency.name}@${dependency.version} license ${license} is not in the policy.`
  };
}

export function renderThirdPartyNotices(classifiedDependencies, generatedAt = new Date().toISOString()) {
  const lines = [
    '# Third-Party Notices',
    '',
    `Generated: ${generatedAt}`,
    '',
    '| Ecosystem | Package | Version | License | Status | Review |',
    '|---|---|---|---|---|---|'
  ];

  for (const dependency of [...classifiedDependencies].sort(compareDependencies)) {
    lines.push(
      `| ${escapeCell(dependency.ecosystem)} | ${escapeCell(dependency.name)} | ${escapeCell(dependency.version)} | ${escapeCell(dependency.effectiveLicense ?? dependency.license)} | ${escapeCell(dependency.status)} | ${escapeCell(dependency.review?.reason ?? '')} |`
    );
  }

  lines.push('');
  return `${lines.join('\n')}`;
}

export function runLicenseCheck(options = {}) {
  const policyPath = path.resolve(options.policyPath ?? path.join(repoRoot, 'tools', 'license-policy.json'));
  const outputDir = path.resolve(options.outputDir ?? path.join(repoRoot, 'artifacts', 'dependency-licenses'));
  const policy = readJson(policyPath);
  const dependencies = [];

  if (!options.skipNpm) {
    const npmLockPaths = options.npmLockPaths ?? [
      path.join(repoRoot, 'apps', 'web', 'package-lock.json'),
      path.join(repoRoot, 'apps', 'validatedscale', 'package-lock.json')
    ];
    for (const npmLockPath of npmLockPaths) {
      dependencies.push(...parseNpmLockDependencies(readJson(npmLockPath)));
    }
  }

  if (!options.skipDotnet) {
    dependencies.push(...collectDotnetDependencies(options.solutionPath ?? path.join(repoRoot, 'Platform.slnx')));
  }

  const classified = dependencies.map((dependency) => classifyDependency(dependency, policy)).sort(compareDependencies);
  const issues = classified.filter((dependency) => dependency.issue).map((dependency) => dependency.issue);

  mkdirSync(outputDir, { recursive: true });
  writeFileSync(
    path.join(outputDir, 'dependency-licenses.json'),
    `${JSON.stringify(
      {
        generatedAt: new Date().toISOString(),
        policyPath: path.relative(repoRoot, policyPath).replaceAll('\\', '/'),
        issues,
        dependencies: classified
      },
      null,
      2
    )}\n`
  );
  writeFileSync(path.join(outputDir, 'THIRD-PARTY-NOTICES.md'), renderThirdPartyNotices(classified));

  return { dependencies: classified, issues, outputDir };
}

function collectDotnetDependencies(solutionPath) {
  const rawOutput = execFileSync('dotnet', buildDotnetListPackageArgs(solutionPath), {
    cwd: repoRoot,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe']
  });
  const jsonStart = rawOutput.indexOf('{');
  if (jsonStart < 0) {
    throw new Error('dotnet list package did not return JSON.');
  }

  const report = JSON.parse(rawOutput.slice(jsonStart));
  const byKey = new Map();

  for (const project of report.projects ?? []) {
    for (const framework of project.frameworks ?? []) {
      for (const packageGroup of ['topLevelPackages', 'transitivePackages']) {
        for (const packageInfo of framework[packageGroup] ?? []) {
          const version = packageInfo.resolvedVersion ?? packageInfo.requestedVersion ?? 'UNKNOWN';
          const key = `nuget:${packageInfo.id}@${version}`;
          if (byKey.has(key)) {
            continue;
          }

          byKey.set(key, {
            ecosystem: 'nuget',
            name: packageInfo.id,
            version,
            license: readNugetLicense(packageInfo.id, version),
            source: path.relative(repoRoot, project.path).replaceAll('\\', '/')
          });
        }
      }
    }
  }

  return [...byKey.values()].sort(compareDependencies);
}

export function buildDotnetListPackageArgs(solutionPath) {
  // SDK 9.0.3xx dropped '--no-restore' from 'dotnet list package'.
  return ['list', solutionPath, 'package', '--include-transitive', '--format', 'json'];
}

function readNugetLicense(packageId, version) {
  const packageRoot = process.env.NUGET_PACKAGES ?? path.join(homedir(), '.nuget', 'packages');
  const packagePath = path.join(packageRoot, packageId.toLowerCase(), version);
  const nuspecPath = path.join(packagePath, `${packageId.toLowerCase()}.nuspec`);

  if (!existsSync(nuspecPath)) {
    return 'MISSING';
  }

  return parseNugetLicenseFromNuspec(readFileSync(nuspecPath, 'utf8'));
}

function findReview(dependency, license, policy) {
  for (const review of policy.reviewedDependencies ?? []) {
    if (review.ecosystem !== dependency.ecosystem) {
      continue;
    }

    if (review.license && review.license !== license) {
      continue;
    }

    if (review.version && review.version !== dependency.version) {
      continue;
    }

    if (review.name && review.name === dependency.name) {
      return review;
    }

    if (review.namePattern && new RegExp(review.namePattern).test(dependency.name)) {
      return review;
    }
  }

  return null;
}

function matchesForbiddenLicense(license, forbiddenPatterns) {
  const upperLicense = license.toUpperCase();
  return forbiddenPatterns.some((pattern) => {
    const upperPattern = pattern.toUpperCase();

    if (upperPattern === 'GPL-') {
      return upperLicense.includes('GPL-') && !upperLicense.includes('LGPL-') && !upperLicense.includes(' WITH ');
    }

    return upperLicense.includes(upperPattern);
  });
}

function matchesLicenseExpression(license, expressions) {
  return expressions.includes(license);
}

function normalizeLicense(license) {
  if (typeof license === 'string') {
    return license.trim() || 'MISSING';
  }

  if (Array.isArray(license)) {
    return license.map((item) => normalizeLicense(item)).join(' OR ');
  }

  if (license && typeof license === 'object' && typeof license.type === 'string') {
    return license.type.trim() || 'MISSING';
  }

  return 'MISSING';
}

function readJson(filePath) {
  return JSON.parse(readFileSync(filePath, 'utf8'));
}

function compareDependencies(left, right) {
  return (
    left.ecosystem.localeCompare(right.ecosystem) ||
    left.name.localeCompare(right.name) ||
    left.version.localeCompare(right.version)
  );
}

function escapeCell(value) {
  return String(value ?? '').replaceAll('|', '\\|').replaceAll('\n', ' ');
}

function parseArgs(argv) {
  const options = {};

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];
    if (arg === '--skip-dotnet') {
      options.skipDotnet = true;
    } else if (arg === '--skip-npm') {
      options.skipNpm = true;
    } else if (arg === '--policy') {
      options.policyPath = argv[++index];
    } else if (arg === '--output-dir') {
      options.outputDir = argv[++index];
    } else if (arg === '--solution') {
      options.solutionPath = argv[++index];
    } else if (arg === '--npm-lock') {
      options.npmLockPaths = [...(options.npmLockPaths ?? []), argv[++index]];
    } else {
      throw new Error(`Unknown argument: ${arg}`);
    }
  }

  return options;
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  try {
    const result = runLicenseCheck(parseArgs(process.argv.slice(2)));
    console.log(`Checked ${result.dependencies.length} dependencies.`);
    console.log(`Wrote ${path.relative(repoRoot, result.outputDir).replaceAll('\\', '/')} attribution artifacts.`);

    if (result.issues.length > 0) {
      console.error('License gate failed:');
      for (const issue of result.issues) {
        console.error(`- ${issue}`);
      }
      process.exitCode = 1;
    }
  } catch (error) {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  }
}
