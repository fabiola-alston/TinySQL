param (
    [Parameter(Mandatory = $true)]
    [string]$IP,
    [Parameter(Mandatory = $true)]
    [int]$Port
)

$eomToken = "<EOM>";
$ipEndPoint = [System.Net.IPEndPoint]::new([System.Net.IPAddress]::Parse($IP), $Port)

function Send-Message {
    param (
        [System.Net.Sockets.Socket]$client,
        [string]$message
    )
    $message += "<EOM>"; #Append token to indicate end of message
    $messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
    $client.Send($messageBytes, [System.Net.Sockets.SocketFlags]::None)
}

function Receive-Message {
    param (
        [System.Net.Sockets.Socket]$client
    )
    $stringBuilder = New-Object System.Text.StringBuilder
    $eom = $false
    do {
        $buffer = New-Object byte[] 1024
        $received = $client.Receive($buffer, [System.Net.Sockets.SocketFlags]::None)
        $response = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $received)
        if ($response.EndsWith($eomToken)) {
            $eom = $true;
            $response = $response.Replace($eomToken, "")
        }
        [void]$stringBuilder.Append($response)
    } while ($eom -eq $false -and $response.Length -gt -1);

    return $stringBuilder;
}

function Send-SQLCommand {
    param (
        [string]$command
    )
    $client = New-Object System.Net.Sockets.Socket($ipEndPoint.AddressFamily, [System.Net.Sockets.SocketType]::Stream, [System.Net.Sockets.ProtocolType]::Tcp)
    $client.Connect($ipEndPoint)
    $requestObject = [PSCustomObject]@{
        RequestType = 0;
        RequestBody = $command
    }
    Write-Host -ForegroundColor Green "Sending command: $command"

    $jsonMessage = ConvertTo-Json -InputObject $requestObject
    Send-Message -client $client -message $jsonMessage
    $response = Receive-Message -client $client

    Write-Host -ForegroundColor Green "Response received: $response"
    
    $responseObject = ConvertFrom-Json -InputObject $response
    Write-Output $responseObject
    $client.Shutdown([System.Net.Sockets.SocketShutdown]::Both)
    $client.Close()
}

function Execute-MyQuery {
    param (
        [string]$file
    )

    $content = Get-Content $file -Raw
    $content = $content -replace '\s+', ' '
    $sqlCommands = $content -split ';\s*'

    Write-Host "Ejecutando script completo." -ForegroundColor Yellow
    
    $totalExecutionTime = Measure-Command {
        foreach ($command in $sqlCommands) {
            $command = $command.Trim()
            if (-not [string]::IsNullOrWhiteSpace($command)) {
                Send-SQLCommand -command $command
                # Write-Host $command
            }
        }
    }

    Write-Host -ForegroundColor Cyan "Tiempo total de ejecucion del script: $($totalExecutionTime.TotalSeconds) segundos."
}

#. .\tinysqlclient.ps1 -IP 127.0.0.1 -Port 11000
# Execute-MyQuery -file testQuery.txt