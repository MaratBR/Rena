# Rena

Rena - is a project creation utility that I wrote as an exercise to learn Golang a little. It uses golang's [`text/template`](https://golang.org/pkg/text/template/) package under the hood.

Rena is simle: it copies everything from your input directory to the output directory and also inserts your custom data along the way.


## Examples

Go to `examples` for... well... examples, duh.

```bash
./build/Rena_0.0.1 -o ./examples/aspdotnet/output --cfg ./examples/aspdotnet/.rena.yaml 
```

## Notes
* Input folder (or working directory) specified in .rena.yaml file in `baseDir` key (see `./examples/aspdotnet/.rena.yaml` for an example)

## TODO

* Make a better README and explain all features in more detail.