#requires -Version 7.0
[CmdletBinding()]
param([string]$BaseUrl = 'http://localhost:8080')

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')
$checks = [System.Collections.Generic.List[string]]::new()

function Assert-Check([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "FAILED: $Name" }
    $checks.Add($Name)
    Write-Host "PASS: $Name"
}

function Send-Api {
    param([string]$Path, [string]$Token, [string]$Method = 'GET', $Body,
          [string]$Accept = 'application/json', [hashtable]$Headers = @{}, [string]$RawBody)
    $requestHeaders = @{ Accept = $Accept }
    foreach ($key in $Headers.Keys) { $requestHeaders[$key] = $Headers[$key] }
    if ($Token) { $requestHeaders.Authorization = "Bearer $Token" }
    $args = @{ Uri = "$BaseUrl$Path"; Method = $Method; Headers = $requestHeaders;
               SkipHttpErrorCheck = $true; TimeoutSec = 30 }
    if ($PSBoundParameters.ContainsKey('Body')) {
        $args.Body = ConvertTo-Json -InputObject $Body -Depth 15 -Compress
        $args.ContentType = 'application/json'
    }
    if ($PSBoundParameters.ContainsKey('RawBody')) {
        $args.Body = $RawBody
        $args.ContentType = 'application/json'
    }
    $r = Invoke-WebRequest @args
    $json = $null
    try { $json = $r.Content | ConvertFrom-Json -AsHashtable } catch { }
    return @{ Status = [int]$r.StatusCode; Json = $json; Raw = [string]$r.Content; Headers = $r.Headers }
}

$health = Send-Api '/health'
Assert-Check ($health.Status -eq 200) 'Health endpoint'
$admin = Send-Api '/api/v1/auth/login' -Method POST -Body @{username='admin';password='Admin@123'}
$student = Send-Api '/api/v1/auth/login' -Method POST -Body @{username='student';password='Student@123'}
Assert-Check ($admin.Status -eq 200 -and $admin.Json.data.expiresIn -gt 0) 'Admin login'
Assert-Check ($student.Status -eq 200) 'Student login'
$adminToken = $admin.Json.data.accessToken
$studentToken = $student.Json.data.accessToken
Assert-Check (($adminToken -split '\.').Count -eq 3) 'JWT has three parts'
foreach ($creds in @(@{username='admin';password='wrong'}, @{username='unknown';password='Admin@123'}, @{username='admin';password='x'})) {
    $r = Send-Api '/api/v1/auth/login' -Method POST -Body $creds
    Assert-Check ($r.Status -eq 401 -and -not $r.Json.success) "Invalid credentials: $($creds.username)/$($creds.password)"
}
foreach ($version in @('v1','v2')) {
    $swagger = Send-Api "/swagger/$version/swagger.json"
    Assert-Check ($swagger.Status -eq 200 -and $swagger.Json.components.securitySchemes.Bearer.scheme -eq 'bearer') "Swagger $version Bearer support"
    Assert-Check ($swagger.Json.paths.ContainsKey("/api/$version/auth/login")) "Swagger $version auth route"
    foreach ($resource in @('students','courses','subjects','semesters','enrollments')) {
        $r = Send-Api "/api/$version/$resource" $studentToken
        Assert-Check ($r.Status -eq 200 -and $r.Json.success -and $r.Json.pagination.totalItems -gt 0) "$version $resource list and paging"
        $r = Send-Api "/api/$version/$resource/1" $adminToken
        Assert-Check ($r.Status -eq 200 -and $r.Json.success) "$version $resource detail"
    }
}
$r = Send-Api '/api/v1/students'
Assert-Check ($r.Status -eq 401) 'Anonymous access rejected'
$r = Send-Api '/api/v1/students' 'not-a-token'
Assert-Check ($r.Status -eq 401) 'Garbage JWT rejected'
$parts = $adminToken.Split('.')
$tampered = $parts[0] + '.' + $parts[1] + '.' + $(if ($parts[2][0] -eq 'A') {'B'} else {'A'}) + $parts[2].Substring(1)
$r = Send-Api '/api/v1/students' $tampered
Assert-Check ($r.Status -eq 401) 'Tampered signature rejected'
$r = Send-Api '/api/v1/users' $studentToken
Assert-Check ($r.Status -eq 403) 'Student forbidden from users endpoint'
$r = Send-Api '/api/v1/users' $adminToken
Assert-Check ($r.Status -eq 200 -and $r.Json.data.Count -eq 2 -and $r.Raw -notmatch 'passwordHash') 'Admin can list users without password hashes'
$r = Send-Api '/api/v1/students' $studentToken -Method POST -Body @{}
Assert-Check ($r.Status -eq 403) 'Student cannot create students'
$r = Send-Api '/api/v1/students' $adminToken -Accept 'application/xml'
Assert-Check ($r.Status -eq 200 -and $r.Headers['Content-Type'][0] -match 'application/xml') 'XML content negotiation'
$xml = [xml]$r.Raw
Assert-Check ($xml.response.data.item.Count -gt 0) 'XML body parses and contains records'
$r = Send-Api '/api/v1/students?size=100&expand=enrollments' $adminToken -Accept 'application/xml'
$xml = [xml]$r.Raw
Assert-Check ($r.Status -eq 200 -and $xml.response.data.item.Count -ge 50) 'Large XML response with expansion'
$r = Send-Api '/api/v1/students' $adminToken -Accept 'application/pdf'
Assert-Check ($r.Status -eq 406) 'Unsupported Accept returns 406'
$r = Send-Api '/api/v1/students' $adminToken -Headers @{'X-Request-Id'='lab2-check'}
Assert-Check ($r.Headers['X-Request-Id'][0] -eq 'lab2-check') 'X-Request-Id echoed'
$r = Send-Api '/api/v1/courses/1/students' $adminToken
Assert-Check ($r.Status -eq 200 -and $r.Json.data.Count -gt 0) 'Nested course students'
$r = Send-Api '/api/v1/students?search=nguyen&sort=fullName&page=1&size=3&fields=studentId,fullName' $adminToken
Assert-Check ($r.Status -eq 200 -and $r.Json.data.Count -eq 3 -and $r.Json.data[0].Count -eq 2) 'Search, sort, paging and field selection combined'
$r = Send-Api '/api/v1/enrollments?size=2&expand=student,course' $adminToken
Assert-Check ($r.Status -eq 200 -and $null -ne $r.Json.data[0].student -and $null -ne $r.Json.data[0].course) 'Enrollment expansion'
foreach ($item in @(@('students','studentCode,age'), @('courses','courseId,enrollmentsCount'), @('semesters','semesterId,coursesCount'), @('subjects','subjectCode,displayName'), @('enrollments','enrollmentId,semesterName'))) {
    $r = Send-Api "/api/v2/$($item[0])?size=2&fields=$($item[1])" $adminToken
    Assert-Check ($r.Status -eq 200 -and $r.Json.data[0].Count -eq 2) "V2 new field selection: $($item[0])"
}
foreach ($query in @('page=0','size=101','page=2147483647','page=abc','sort=unknown','fields=unknown','expand=unknown')) {
    $r = Send-Api "/api/v1/students?$query" $adminToken
    Assert-Check ($r.Status -eq 400 -and -not $r.Json.success) "Query validation: $query"
}
$r = Send-Api '/api/v1/students/abc' $adminToken
Assert-Check ($r.Status -in 400,404) 'Integer route constraint'
$r = Send-Api '/api/v1/students/2147483647' $adminToken
Assert-Check ($r.Status -eq 404 -and -not $r.Json.success) 'Missing student returns envelope'
$r = Send-Api '/api/v1/students' $adminToken -Method POST -RawBody '{"fullName":'
Assert-Check ($r.Status -eq 400 -and $r.Json.errors.Count -gt 0) 'Malformed JSON returns field errors'
$valid = @{fullName='Lab2 Integration';email="lab2.$([guid]::NewGuid().ToString('N'))@test.local";dateOfBirth='2003-01-15T00:00:00';studentCode='SE999999'}
foreach ($body in @(@{}, ($valid + @{invalidProperty=-1}))) {
    $r = Send-Api '/api/v1/students' $adminToken -Method POST -Body $body
    Assert-Check ($r.Status -eq 400 -and $r.Json.errors.Count -gt 0) 'Missing or unknown input rejected'
}
foreach ($badField in @{email='invalid';studentCode='XX123';phoneNumber='abc';dateOfBirth='0001-01-01T00:00:00'}.GetEnumerator()) {
    $body = $valid.Clone(); $body[$badField.Key] = $badField.Value
    $r = Send-Api '/api/v1/students' $adminToken -Method POST -Body $body
    Assert-Check ($r.Status -eq 400 -and $r.Json.errors.Count -gt 0) "Request validation: $($badField.Key)"
}
$createdId = $null
try {
    $valid.Remove('studentCode')
    $r = Send-Api '/api/v1/students' $adminToken -Method POST -Body $valid
    Assert-Check ($r.Status -eq 201) 'Create student'
    $createdId = $r.Json.data.studentId
    Assert-Check ($r.Headers.Location[0] -match "/api/v1/students/$createdId$") 'Named route generates versioned Location'
    $r = Send-Api "/api/v2/students/$createdId" $adminToken
    $valid.studentCode = $r.Json.data.studentCode
    $valid.fullName = 'Lab2 Updated'
    $r = Send-Api "/api/v1/students/$createdId" $adminToken -Method PUT -Body $valid
    Assert-Check ($r.Status -eq 200 -and $r.Json.data.fullName -eq 'Lab2 Updated') 'Update with generated six-digit student code'
    $valid.Remove('studentCode')
    $r = Send-Api "/api/v1/students/$createdId" $adminToken -Method PUT -Body $valid
    Assert-Check ($r.Status -eq 200) 'Update without optional student code'
} finally {
    if ($createdId) {
        $r = Send-Api "/api/v1/students/$createdId" $adminToken -Method DELETE
        Assert-Check ($r.Status -eq 200) 'Delete test student'
    }
}
$r = Send-Api '/api/v1/auth/refresh-token' -Method POST -Body @{refreshToken=$admin.Json.data.refreshToken}
Assert-Check ($r.Status -eq 200 -and $r.Json.data.accessToken -ne $adminToken) 'Refresh produces a new access token'
$refreshed = $r.Json.data
$r = Send-Api '/api/v1/students' $refreshed.accessToken
Assert-Check ($r.Status -eq 200) 'Refreshed access token works'
$r = Send-Api '/api/v1/auth/refresh-token' -Method POST -Body @{refreshToken=$admin.Json.data.refreshToken}
Assert-Check ($r.Status -eq 401) 'Replayed refresh token rejected'
$r = Send-Api '/api/v1/auth/refresh-token' -Method POST -Body @{refreshToken=$refreshed.refreshToken}
Assert-Check ($r.Status -eq 401) 'Replay revokes remaining refresh sessions'
$r = Send-Api '/api/v1/auth/refresh-token' -Method POST -Body @{refreshToken='invalid'}
Assert-Check ($r.Status -eq 401) 'Unknown refresh token rejected'

$resultFolder = Join-Path $PSScriptRoot '../test-results'
New-Item -ItemType Directory -Force $resultFolder | Out-Null
@{passed=$checks.Count;checks=@($checks);timestamp=[datetime]::UtcNow.ToString('o')} |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $resultFolder 'integration.json') -Encoding utf8
Write-Host "All $($checks.Count) integration checks passed."
