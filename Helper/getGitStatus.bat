@echo off
for /f "delims=" %%a in ('git rev-parse HEAD') do @set revisionHash=%%a
for /f "delims=" %%a in ('git rev-parse --abbrev-ref HEAD') do @set branch=%%a
start /wait %1\generateBuildProperties.vbs %2 666 %branch% %revisionHash%
