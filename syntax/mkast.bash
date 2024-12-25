#!/usr/bin/env bash

set -eu
cd "$(dirname "${BASH_SOURCE[0]}")"

~/Documents/Programmation/Projets/ast-gen/mkast.py -c config.yml nodes.yml