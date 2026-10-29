@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

where python >nul 2>nul || (echo Chua co Python. Cai Python 3.11+ tu python.org (tick "Add to PATH") roi chay lai. & pause & exit /b 1)

if not exist .venv (
  python -m venv .venv || (echo Tao venv that bai & pause & exit /b 1)
)
call .venv\Scripts\activate.bat
python -m pip install --upgrade pip >nul
pip install -r requirements.txt || (echo Cai thu vien that bai & pause & exit /b 1)

set ICON=
if exist assets\musewin.ico set ICON=--icon assets\musewin.ico

pyinstaller --noconfirm --clean --onefile --windowed --name MuseWin %ICON% ^
  --add-data "index.html;." ^
  musewin.py || (echo Build that bai & pause & exit /b 1)

echo.
echo Xong: dist\MuseWin.exe
pause
