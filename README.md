# Pulse

### Run tests:
`dotnet test`
### Produce coverage report
`dotnet test --coverage --coverage-output-format cobertura`
### Convert it to HTML
`reportgenerator "-reports:**/*.cobertura.xml" "-targetdir:CoverageReport"`
