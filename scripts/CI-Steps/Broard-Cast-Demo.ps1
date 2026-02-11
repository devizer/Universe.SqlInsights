Function BroadCast-Variables() {
    try {
        # Сигнатура Win32 API
        $signature = '[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
                      public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);'

        # Добавляем тип (с проверкой, чтобы избежать ошибок при повторном вызове)
        if (-not ([System.Management.Automation.PSTypeName]"Win32.NativeMethods").Type) {
            Add-Type -MemberDefinition $signature -Name "NativeMethods" -Namespace "Win32"
        }

        $result = [UIntPtr]::Zero
        # Используем [ref] вместо out
        $ret = [Win32.NativeMethods]::SendMessageTimeout([IntPtr]0xffff, 0x001A, [UIntPtr]::Zero, "Environment", 0x02, 1000, [ref]$result)
        
        Write-Line -TextGreen "Bradcast variables success, SendMessageTimeout() --> $ret, [ref] result = $result"
    } 
    catch {
        Write-Line -TextRed "Bradcast variables failed: $($_.Exception.Message)"
    }
}
# BroadCast-Variables
# OK: Bradcast variables success, SendMessageTimeout() --> 1, [ref] result = 0
