#!/usr/bin/python3
# -*- coding: utf-8 -*-

import sys, os, shutil, json
from subprocess import call
import distutils.dir_util

import configparser
from distutils.version import LooseVersion
from urllib.request import Request, urlopen

GlobalScriptPath = os.path.dirname(os.path.realpath(__file__)).replace('\\','/')
GlobalReleasePath = GlobalScriptPath + "/BuildRelease/PortableApps/MachinaTrader"
GlobalGitPath = GlobalScriptPath + "/MachinaTraderGit"
GlobalSourceFolder = GlobalScriptPath

GlobalConfigFileName=os.path.basename(__file__).replace('.py','.json')
GlobalConfigFile=GlobalScriptPath + "/" + GlobalConfigFileName
portableAppsPath=os.path.dirname(GlobalScriptPath)

if os.path.exists(GlobalConfigFile):
    with open(GlobalConfigFile) as data_file:
        GlobalConfig = json.load(data_file)
else:
    print("Config not found - Creating")
    GlobalConfig = {}
    GlobalConfig["BuildOptions"] = {};
    GlobalConfig["BuildOptions"]["BuildSetupFolders"] = True
    GlobalConfig["BuildOptions"]["BuildAddPlugins"] = True
    GlobalConfig["BuildOptions"]["BuildEnableAllPlugins"] = True
    GlobalConfig["BuildOptions"]["BuildCompile"] = True
    GlobalConfig["BuildOptions"]["BuildCreateInstaller"] = True
    GlobalConfig["EnabledPlugins"] = []

# What to do
BuildSetupFolders = GlobalConfig["BuildOptions"]["BuildSetupFolders"]
BuildAddPlugins = GlobalConfig["BuildOptions"]["BuildAddPlugins"]
BuildEnableAllPlugins = GlobalConfig["BuildOptions"]["BuildEnableAllPlugins"]
BuildCompile = GlobalConfig["BuildOptions"]["BuildCompile"]
BuildCreateInstaller = GlobalConfig["BuildOptions"]["BuildCreateInstaller"]

ConfigValidPlugins=GlobalConfig["EnabledPlugins"]

if (BuildSetupFolders):
    print('-------------------------------------------------------------------------')
    print('Setup Folders')
    print('-------------------------------------------------------------------------')
    if not os.path.isdir(GlobalSourceFolder + '/BuildRelease/App'):
        print ('Source Folder not found -> Fallback to Git Folder')
        GlobalSourceFolder = GlobalGitPath
    print (GlobalSourceFolder)
    shutil.rmtree(GlobalReleasePath + "/App", ignore_errors=True)
    os.makedirs(GlobalReleasePath, exist_ok=True)
    os.makedirs(GlobalReleasePath +'/App/Plugins', exist_ok=True)
    distutils.dir_util.copy_tree(GlobalSourceFolder + '/BuildRelease/App/AppInfo', GlobalReleasePath + '/App/AppInfo')
    shutil.copy2(GlobalSourceFolder + '/BuildRelease/AppAdditions/help.html', GlobalReleasePath + '/help.html')
    distutils.dir_util.copy_tree(GlobalSourceFolder + '/MachinaTrader/wwwroot', GlobalReleasePath + '/App/wwwroot')

if BuildEnableAllPlugins:
    ConfigValidPlugins=[]
    for root, directories, files in os.walk(GlobalSourceFolder):
        for pluginFolder in directories:
            if pluginFolder.startswith('MachinaTrader.Plugin.'):
                ConfigValidPlugins.append(pluginFolder)

if (BuildAddPlugins):
    print('-------------------------------------------------------------------------')
    print('Add Plugins')
    print('-------------------------------------------------------------------------')

    for validPlugin in ConfigValidPlugins:
        try:
            distutils.dir_util.copy_tree(GlobalSourceFolder + '/' + validPlugin + '/wwwroot', GlobalReleasePath + '/App/Plugins/' + validPlugin+ '/wwwroot')
        except:
            print("Error: " + validPlugin + " dont contains a wwwroot Folder")

if (BuildCompile):
    print('-------------------------------------------------------------------------')
    print('Cleanup Project File')
    print('-------------------------------------------------------------------------')

    shutil.copy2(GlobalSourceFolder + '/MachinaTrader/MachinaTrader.csproj', GlobalSourceFolder + '/MachinaTrader/MachinaTrader.csproj.bak')
    f = open(GlobalSourceFolder + '/MachinaTrader/MachinaTrader.csproj','r+')
    d = f.readlines()
    f.seek(0)

    for i in d:
        if 'MachinaTrader.Globals' in i:
            f.write(i)
            #Add all enabled Plugins
            for validPlugin in ConfigValidPlugins:
                f.write('<ProjectReference Include="..\\' + validPlugin + '\\' + validPlugin + '.csproj" />\r\n')
        elif 'MachinaTrader.Plugin.' not in i:
            f.write(i)

    f.truncate()
    f.close()

    print('-------------------------------------------------------------------------')
    print('Compile')
    print('-------------------------------------------------------------------------')

    os.chdir(GlobalSourceFolder + '/MachinaTrader')
    os.system('dotnet restore')

    if sys.platform == "win32":
        os.system('dotnet publish --framework netcoreapp2.1 --self-contained --runtime win-x64 --output ' + GlobalReleasePath + '/App')
        #Copy Launcher
        shutil.copy2(GlobalSourceFolder + '/BuildRelease/AppAdditions/MachinaTraderLauncher.exe', GlobalReleasePath + '/MachinaTraderLauncher.exe')
    if sys.platform =="linux":
        os.system('dotnet publish --framework netcoreapp2.1 --self-contained --runtime linux-x64 --output ' + GlobalReleasePath + '/App')

    # Restore real project
    os.remove(GlobalSourceFolder + '/MachinaTrader/MachinaTrader.csproj')
    shutil.copy2(GlobalSourceFolder + '/MachinaTrader/MachinaTrader.csproj.bak', GlobalSourceFolder + '/MachinaTrader/MachinaTrader.csproj')

# Windows: Release Path is scriptpath + "/PortableApps"
if BuildCreateInstaller:

    # Check if needed Build Tools are installed, we DONT check for netcore/vsbuild tools because they are hard dependency
    if not os.path.isdir(GlobalSourceFolder + "/BuildRelease"):
        os.makedirs(GlobalSourceFolder + "/BuildRelease", exist_ok=True)
    if not os.path.isdir(GlobalSourceFolder + "/BuildRelease"):
        os.makedirs(GlobalSourceFolder + "/BuildRelease", exist_ok=True)
    if not os.path.isfile(GlobalSourceFolder + "/BuildRelease/MachinaCore.comAppInstaller/PortableApps.comInstaller.exe"):
        print("Warning Build Tools dont exist - Downloading")
        os.chdir(GlobalSourceFolder + "/BuildRelease")
        os.system('git clone https://github.com/MachinaCore/MachinaCore.comAppInstaller.git ' + GlobalScriptPath + "/BuildRelease/AppInstaller")

    os.chdir(GlobalScriptPath)

    config = configparser.ConfigParser()
    config.read(GlobalSourceFolder + '/BuildRelease/App/AppInfo/appinfo.ini')
    fileName = config['Details']['AppID']
    fileVersion = config['Version']['PackageVersion']
    installerFileName = fileName + "_" + fileVersion + ".paf.exe"

    # Make sure data folder is deleted for release
    shutil.rmtree(GlobalReleasePath + "/App/Data", ignore_errors=True)

    # Create 7-zip
    os.system('7z.exe a -r -t7z -mx=9 '+ fileName +'_'+ fileVersion +'.7z ' + GlobalReleasePath.replace("/","\\") + '\\*')

    # Create Installer
    os.system(GlobalSourceFolder + '/BuildRelease/AppInstaller/PortableApps.comInstaller.exe "'+ GlobalReleasePath.replace("/","\\") + '"')
