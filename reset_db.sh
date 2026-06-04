#!/bin/bash
set -e
docker volume rm fabricore-postgres
docker volume create fabricore-postgres