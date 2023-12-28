@echo off
echo Welcome to Apollo [Alpha]

:menu
echo 1. AesKeys
echo 2. Cosmetics 
echo 3. News (soon)
echo 4. Quit

set /p choice=Write what you want to choose:
if "%choice%"=="aes" goto aeskeys
if "%choice%"=="cos" goto cosmetics
if "%choice%"=="quit" goto quit

echo Invalid selection. Please look in readme.
goto menu

:aeskeys
echo AesKeys
start npm run aes
goto end

:cosmetics
echo Cosmetics Menu
goto cos_menu 

:cos_menu
echo 1. Latest Cosmetics (soon)
echo 2. All Cosmetics
echo 3. Back

set /p choice=Please select an option:
if "%choice%"=="all" goto cos_all
if "%choice%"=="back" goto back

:cos_all
echo All Cosmetics.
start npm run cos_all
goto end

:quit
echo The program is terminated.
goto end

:end
