Add-Type -AssemblyName Microsoft.VisualBasic
Add-Type -AssemblyName System.Windows.Forms

$unityProcess = Get-Process | Where-Object { $_.Name -like "*Unity*" -and $_.MainWindowTitle -ne "" } | Select-Object -First 1

if ($unityProcess) {
    $uPid = $unityProcess.Id
    Write-Output "Found Unity Editor process: $($unityProcess.Name) with PID: $uPid, Title: $($unityProcess.MainWindowTitle)"
    try {
        [Microsoft.VisualBasic.Interaction]::AppActivate($uPid)
        Start-Sleep -Milliseconds 400
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
        Write-Output "Successfully activated and sent ENTER!"
    } catch {
        Write-Output "Failed to activate or send keys: $_"
    }
} else {
    Write-Output "Unity Editor process not found"
}
