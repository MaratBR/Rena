{{.params.projectName}}
======================

Welcome to _{{.params.projectName}}_. This is my new project and it's awesome.

Feel free to contribute or else.

## Here is our main goals


{{range $_, $v := .params.goals}}* {{$v}}
{{end}}