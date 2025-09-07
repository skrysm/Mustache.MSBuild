#!/bin/sh -e
#
# Call this script when the current repository contains git submodules and you forgot to clone it
# with "--recurse-submodules". (The end effect will be the same as if you had clone this repository
# with "--recurse-submodules".)
#
echo -e "\033[1;36mCloning submodules...\033[0m"
echo

git submodule init
git submodule update
