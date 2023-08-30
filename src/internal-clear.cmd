@echo off
for /d %%d in (*) DO (
  if Exist %%d\bin rd /q /s %%d\bin
  if Exist %%d\obj rd /q /s %%d\obj
)
if Exist packages rd /q /s packages