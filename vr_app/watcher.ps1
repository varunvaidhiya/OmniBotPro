while ($true) {
    $coreCache = Get-ChildItem -Path "C:\Users\varun\Desktop\Projects\OmniBotPro\vr_app\Library\PackageCache" -Filter "com.meta.xr.sdk.core@*" -Directory | Select-Object -First 1
    $intCache = Get-ChildItem -Path "C:\Users\varun\Desktop\Projects\OmniBotPro\vr_app\Library\PackageCache" -Filter "com.meta.xr.sdk.interaction@*" -Directory | Select-Object -First 1

    if ($coreCache -and $intCache) {
        Copy-Item -Recurse -Path $coreCache.FullName -Destination "C:\Users\varun\Desktop\Projects\OmniBotPro\vr_app\Packages\com.meta.xr.sdk.core"
        Copy-Item -Recurse -Path $intCache.FullName -Destination "C:\Users\varun\Desktop\Projects\OmniBotPro\vr_app\Packages\com.meta.xr.sdk.interaction"

        $f1 = "C:\Users\varun\Desktop\Projects\OmniBotPro\vr_app\Packages\com.meta.xr.sdk.core\Scripts\MCPBridge\Tools\Reflection.cs"
        if (Test-Path $f1) {
            $c1 = Get-Content $f1 -Raw
            $c1 = $c1 -replace 'unchecked\(\(int\)UnityEngine\.EntityId\.ToULong\(unityObject\.GetEntityId\(\)\)\)', 'unityObject.GetHashCode()'
            Set-Content -Path $f1 -Value $c1
        }

        $f2 = "C:\Users\varun\Desktop\Projects\OmniBotPro\vr_app\Packages\com.meta.xr.sdk.interaction\Editor\OpenXR\OpenXRMigrationWindow.cs"
        if (Test-Path $f2) {
            $c2 = Get-Content $f2 -Raw
            $c2 = $c2 -replace 'unchecked\(\(int\)UnityEngine\.EntityId\.ToULong\(([^.]+)\.GetEntityId\(\)\)\)', '$1.GetHashCode()'
            Set-Content -Path $f2 -Value $c2
        }

        Write-Host "Patched successfully!"
        break
    }
    Start-Sleep -Seconds 2
}
