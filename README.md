# PushMonitorGui

## Overview

Sometimes it's convenient to make multiple commits locally and push them later. The problem is that it's easy to forget to push before shutting down your computer. Later you realize that all your commits are still only on your local machine.

PushMonitorGit helps avoid this situation. It scans repositories and reports commits that have not been pushed to the remote.

## Features

* Recursively scans all directories inside the specified path
* Detects Git repositories with unpushed commits
* Supports Git submodules
* [TortoiseGit](https://tortoisegit.org/) integration

## Example

![PushMonitorGui example output](docs/screenshot.png)
