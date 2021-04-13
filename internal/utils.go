package internal

import (
	"log"
	"os"
	"path/filepath"

	"github.com/mitchellh/go-homedir"
)

func PanicOnError(err error) {
	if err != nil {
		log.Panicln(err)
	}
}

func NormalizePath(path string) string {
	var err error
	path = filepath.Clean(path)
	path, err = homedir.Expand(path)
	PanicOnError(err)
	path = os.ExpandEnv(path)
	return path
}

func mustCloseFile(f *os.File) {
	if err := f.Close(); err != nil {
		panic(err)
	}
}
