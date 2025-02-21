$outputFile = "employees.csv"
"Id,FirstName,LastName,Email,Department,Salary" | Out-File $outputFile

$departments = @("IT", "HR", "Finance", "Marketing", "Sales")
1..10000 | ForEach-Object {
    $firstName = "FirstName$_"
    $lastName = "LastName$_"
    $email = "$firstName.$lastName@company.com"
    $department = $departments[(Get-Random -Maximum 5)]
    $salary = Get-Random -Minimum 50000 -Maximum 100000
    "$_,$firstName,$lastName,$email,$department,$salary" | Add-Content $outputFile
}