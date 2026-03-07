@echo off
echo ========================================
echo FlowWorker 一体化部署打包脚本
echo ========================================
echo.

echo [1/4] 构建前端项目...
cd frontend
call npm run build
if %errorlevel% neq 0 (
    echo 前端构建失败！
    pause
    exit /b 1
)
cd ..
echo 前端构建完成。
echo.

echo [2/4] 清理旧的发布文件...
if exist src\FlowWorker.Api\publish (
    rmdir /s /q src\FlowWorker.Api\publish
)
echo 旧的发布文件已清理。
echo.

echo [3/4] 发布 .NET 项目...
cd src\FlowWorker.Api
dotnet publish -c Release -o ../publish
if %errorlevel% neq 0 (
    echo .NET 发布失败！
    pause
    exit /b 1
)
cd ../..
echo .NET 发布完成。
echo.

echo [4/4] 复制额外的依赖文件...
xcopy "src\FlowWorker.Api\wwwroot\*" "src\FlowWorker.Api\publish\wwwroot\" /E /I /Y
xcopy "src\FlowWorker.Api\bin\Release\net9.0\runtimes\*" "src\FlowWorker.Api\publish\runtimes\" /E /I /Y
copy "src\FlowWorker.Api\bin\Release\net9.0\runtimes\win-x64\native\e_sqlite3.dll" "src\FlowWorker.Api\publish\e_sqlite3.dll"
echo 额外的依赖文件已复制。
echo.

echo ========================================
echo 打包完成！
echo 发布目录: src\FlowWorker.Api\publish
echo ========================================
echo.
echo 运行可执行文件:
echo cd src\FlowWorker.Api\publish
echo FlowWorker.Api.exe
echo.
echo 或者直接运行:
echo dotnet run --project src\FlowWorker.Api\FlowWorker.Api.csproj
echo.

pause