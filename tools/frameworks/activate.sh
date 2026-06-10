#!/usr/bin/env bash

FRAMEWORKS_BIN="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/bin"

if [[ ":$PATH:" != *":$FRAMEWORKS_BIN:"* ]]; then
  export PATH="$FRAMEWORKS_BIN:$PATH"
fi
