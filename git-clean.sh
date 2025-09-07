#!/bin/sh -e

echo -e "\033[1;36mRemoving ignored and untracked files and directories...\033[0m"
echo

# Delete all untracked and ignored files and directories.
# NOTE: We need to use '-x' instead of '-X' here. It's not really clear
#   what's the difference but when using '-X' some directories don't get
#   deleted (for example, the "bin/Debug" directories in C# projects).
git clean -f -d -x .

# Clean all submodules. (The above command only cleans the current repo.)
git submodule foreach --recursive git clean -f -d -x
