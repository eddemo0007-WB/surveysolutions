<#
.Synopsis
	Build script <https://github.com/nightroman/Invoke-Build>
#>

param(
    [string] $Tasks,
    
    [string] $buildNumber = '42',
    [string] $androidKeyStore = $ENV:ANDROID_KEY_STORE,
    [string] $KeystorePassword = $ENV:ANDROID_SIGNING_KEY_PASS ,
    [string] $KeystoreAlias = $ENV:ANDROID_KEY_ALIAS,
    [string] $GoogleMapKey = $NULL,
    [string] $ArcGisKey = $NULL,
    [string] $dockerRegistry = "ghcr.io/surveysolutions",
    [string] $releaseBranch = 'release', # Docker builds will push to release 
    [switch] $noDockerPush 
)

#region Bootstrap

# self invoke as build script
if ($MyInvocation.ScriptName -notlike '*Invoke-Build.ps1') {
    If (-not(Get-InstalledModule InvokeBuild -ErrorAction silentlycontinue)) {
        Install-Module InvokeBuild -Force
    }
    & "Invoke-Build" $Tasks $MyInvocation.MyCommand.Path @PSBoundParameters
    return
}

$tmp = $ENV:TEMP + "/.build"


dotnet tool install gitversion.tool --tool-path $tmp | Out-Null

& "$tmp/dotnet-gitversion" /nofetch > .version
Get-Content .version  | Out-Host
$gitversion = Get-Content .version | ConvertFrom-Json

$isRelease = $gitversion.BranchName -eq $releaseBranch

$version = Get-Content ./src/.version
if ($version.Split('.').Length -eq 2) {
    $version += ".0"
}
$version += "." + $buildNumber
$infoVersion = $version + '-' + $gitversion.EscapedBranchName

$output = "./artifacts"
New-Item -Type Directory $output -ErrorAction SilentlyContinue | Out-Null
$output = Resolve-Path $output

function Compress($folder, $dest) {
    if (Test-Path $dest) {
        Remove-Item $dest
    }

    Compress-Archive $folder/* -DestinationPath $dest
}



# $gitversion | Out-Host
$version | Out-Host
$infoversion | Out-Host

function Set-AndroidXmlResourceValue {
    [CmdletBinding()]
    param (  
        $project,
        [string] $keyName,
        [string] $keyValue
    )    

    $filePath = "$([System.IO.Path]::GetDirectoryName($project))/Resources/values/settings.xml"
    # Log-Message "Updating app resource key in $filePath"

    [xml] $resourceFile = Get-Content -Path $filePath
    $appCenterKey = Select-Xml -xml $resourceFile `
        -Xpath "/resources/string[@name='$keyName']"

    $appCenterKey.Node.InnerText = $keyValue

    $resourceFile.Save($filePath)
}

function Build-Docker($dockerfile, $tags, $arguments = @()) {
    $builder = docker buildx ls | Where-Object { $_.Contains("wb_buildx_builder") }

    if ($builder.Length -eq 0) {
        docker buildx create wb_buildx_builder
    }

    docker buildx use wb_buildx_builder

    $params = @('buildx', 'build'
        '--build-arg', "VERSION=$version", 
        "--build-arg", "INFO_VERSION=$infoVersion"
        # "--build-arg", "APK_FILES=artifacts"
        "--file", $dockerfile
        "--iidfile", "$output\headquarters.id"
        "--label", "org.opencontainers.image.revision=$($ENV:BUILD_VCS_NUMBER)"
        "--label", "org.opencontainers.image.version=$infoVersion"
        "--label", "org.opencontainers.image.url=https://github.com/surveysolutions/surveysolutions"
        "--label", "org.opencontainers.image.source=https://github.com/surveysolutions/surveysolutions"
        "--label", "org.opencontainers.image.vendor=Survey Solutions"
        "--label", "org.opencontainers.image.created=$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ss')"
        "--cache-from", "type=local,src=$tmp/docker"
        "--cache-to", "type=local,dest=$tmp/docker"
        "--progress", "plain"
        if ($noDockerPush.IsPresent) {
            "--load"
        }
        else {
            "--push"
        }
        $tags | ForEach-Object {
            "--tag"
            $_
        }
        $arguments | ForEach-Object {
            $_
        }
        "."
    )

    $params | Join-String -Separator ', ' | Out-Host
    exec { docker $params }
}

function Get-DockerTags($name, $registry = $dockerRegistry) {
    return @(
        "$registry/$name`:$($gitversion.EscapedBranchName)"
        if ($isRelease) {
            $v = [System.Version]::Parse($version)

            "$registry/$name`:$($v.Major).$($v.Minor)"
            "$registry/$name`:$($v.Major).$($v.Minor).$($v.Build)"
            "$registry/$name`:$($v.Major).$($v.Minor).$($v.Build).$($v.Revision)"
        }
    )
}

function Invoke-Android($CapiProject, $apk, $withMaps, $appCenterKey) {
    Set-Alias MSBuild (Resolve-MSBuild)

    Set-AndroidXmlResourceValue $CapiProject "appcenter_key" $AppCenterKey
    Set-AndroidXmlResourceValue $CapiProject "google_maps_api_key" $GoogleMapKey
    Set-AndroidXmlResourceValue $CapiProject "arcgisruntime_key" $ArcGisKey

    $keyStore = [System.IO.Path]::GetTempFileName()
    if ($null -ne $androidKeyStore) {
        $keyStore = [System.IO.Path]::GetTempFileName()
        [System.IO.File]::WriteAllBytes($keyStore, [System.Convert]::FromBase64String($androidKeyStore))
    }

    $params = @(
        $CapiProject
        "/maxcpucount", "/restore", "/nologo"
        "/p:Configuration=Release", "/p:DebugSymbols=False"
        "/p:ExcludeGeneratedDebugSymbol=True"
        "/p:VersionCode=$buildNumber"
        "/p:ExcludeExtensions=$($withMaps -eq $False)"
        "/p:ApkOutputPath=$output/$apk"
        "/target:SignAndroidPackage;MoveApkFile"
        "/verbosity:Quiet"
        "/p:ApkOutputPath=$output/$apk"
        "/bl:$output/$apk.binlog"
        if ($null -ne $androidKeyStore -and $null -ne $KeystorePassword) {
            '/p:AndroidUseApkSigner=true'
            '/p:AndroidKeyStore=True'
            "/p:AndroidSigningKeyAlias=$KeystoreAlias"
            "/p:AndroidSigningKeyPass=$KeystorePassword"
            "/p:AndroidSigningKeyStore=$keyStore"
            "/p:AndroidSigningStorePass=$KeystorePassword"
        }
    )
    
    exec {

        $params | Join-String -Separator ', ' | Out-Host
        msbuild $params
    }
}

#endregion
task frontend {
    exec { 
        Set-Location ./src/UI/WB.UI.Frontend
        npm ci
        npm run build
    }
}

task PackageHq frontend, {
    exec {
        dotnet publish ./src/UI/WB.UI.Headquarters.Core `
            -c Release -r win-x64 -p:Version=$VERSION -p:InformationalVersion=$INFO_VERSION -o $tmp/hq
    }
    Compress $tmp/hq $output/WB.UI.Headquarters.zip
}

task PackageHqOffline frontend, {
    exec {
        dotnet publish ./src/UI/WB.UI.Headquarters.Core `
            /p:PublishSingleFile=true /p:SelfContained=False /p:AspNetCoreHostingModel=outofprocess `
            /p:IncludeAllContentForSelfExtract=true `
            -c Release -r win-x64 -p:Version=$VERSION -p:InformationalVersion=$INFO_VERSION -o $tmp/hq-offline
    }
    New-Item -Type Directory $tmp/hq-prepare -ErrorAction SilentlyContinue | Out-Null
    copy-item $tmp/hq-offline/WB.UI.Headquarters.exe $tmp/hq-prepare
    copy-item $tmp/hq-offline/web.config $tmp/hq-prepare/Web.config
    copy-item $tmp/hq-offline/appsettings.ini $tmp/hq-prepare

    Compress $tmp/hq-prepare $output/WB.UI.Headquarters.Offline.zip
}

task PackageExport {
    exec {
        dotnet publish ./src/Services/Export/WB.Services.Export.Host `
            -c Release -r win-x64 -p:Version=$VERSION -p:InformationalVersion=$INFO_VERSION -o $tmp/export
    }
    Compress $tmp/export $output/WB.Services.Export.zip
}

task PackageWebTester frontend, {
    exec {
        dotnet publish ./src/UI/WB.UI.WebTester `
            -c Release -r win-x64 -p:Version=$VERSION -p:InformationalVersion=$INFO_VERSION -o $tmp/webtester
    }
    Compress $tmp/webtester $output/WB.UI.WebTester.zip
}

task PackageDesigner {
    
    @("$BuildRoot/src/UI/WB.UI.Designer", "$BuildRoot/src/UI/WB.UI.Designer/questionnaire-app") | ForEach-Object {
        Set-Location $_
        npm i | Out-Host
        npm run build | Out-Host
    }
    
    Set-location $BuildRoot
    dotnet publish ./src/UI/WB.UI.Designer `
        -c Release -r win-x64 `
        -p:Version=$VERSION -p:InformationalVersion=$INFO_VERSION `
        -p:SkipSpaBuild=True -o $tmp/Designer

    Compress $tmp/Designer $output/WB.UI.Designer.zip
}

task AndroidInterviewerWithMaps {
    $appCenterKey = $isRelease ? $ENV:APP_CENTER_INTERVIEWER_PROD : $ENV:APP_CENTER_INTERVIEWER_DEV
    Invoke-Android "./src/UI/Interviewer/WB.UI.Interviewer/WB.UI.Interviewer.csproj" "WBCapi.Ext.apk" $true $appCenterKey
}

task AndroidInterviewer {
    $appCenterKey = $isRelease ? $ENV:APP_CENTER_INTERVIEWER_PROD : $ENV:APP_CENTER_INTERVIEWER_DEV
    Invoke-Android "./src/UI/Interviewer/WB.UI.Interviewer/WB.UI.Interviewer.csproj" "WBCapi.apk" $false $appCenterKey
}

task AndroidSupervisor {
    $appCenterKey = $isRelease ? $ENV:APP_CENTER_SUPERVISOR_PROD : $ENV:APP_CENTER_SUPERVISOR_DEV
    Invoke-Android "./src/UI/Supervisor/WB.UI.Supervisor/WB.UI.Supervisor.csproj" "Supervisor.apk" $true $appCenterKey
}

task DockerHq {
    $tags = Get-DockerTags "headquarters"
    $arguments = @() 

    if ($isRelease) {
        $tags += Get-DockerTags "surveysolutions" "surveysolutions"
        $tags += @("--tag", "surveysolutions/surveysolutions:latest")
    }

    if (-not $noDockerPush.IsPresent) {
        $arguments += @(
            "--platform", "linux/amd64,linux/arm64"
        )
    }

    Build-Docker ./docker/Dockerfile.hq $tags $arguments
}

task DockerDesigner {
    Build-Docker ./docker/Dockerfile.designer (Get-DockerTags "designer")
}

task DockerWebTester {
    Build-Docker ./docker/Dockerfile.webtester (Get-DockerTags "webtester")
}

task Android AndroidInterviewer, AndroidInterviewerWithMaps, AndroidSupervisor
task Packages PackageHq, PackageExport, PackageWebTester, PackageHqOffline
task Docker DockerHq, DockerDesigner, DockerWebTester

task . {
    Set-Location $BuildRoot/src/UI/WB.UI.Designer
    npm i
    npm run dev

    Set-Location $BuildRoot/src/UI/WB.UI.Frontend
    npm i
    npm run dev
}
