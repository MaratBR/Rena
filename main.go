package main

import (
	"flag"
	"log"
	"os"
	"path/filepath"

	"github.com/MaratBR/Reno/internal"
)

var (
	cfg *internal.Config
	err error

	dest               string
	noDestClear        bool
	cfgFile            string
	ignoreParserErrors bool
)

func main() {
	// flag parsing
	flag.StringVar(&dest, "o", "", "Output folder")
	flag.BoolVar(&noDestClear, "no-clear", false, "If set, does NOT clear destination folder")
	flag.BoolVar(&noDestClear, "no-parser-errors", false, "If set, doesn't panic when failing to parse any file, just logs it to the stdout")
	flag.StringVar(&cfgFile, "cfg", ".rena.yaml", "Path to the configuration file")
	flag.Parse()

	if dest == "" {
		log.Fatalln("specify output directory with -o [directory]")
	}

	dest, err = filepath.Abs(dest)
	internal.PanicOnError(err)

	if !noDestClear {
		err = os.RemoveAll(dest)
		internal.PanicOnError(err)
	}

	// check flags
	err = os.Mkdir(dest, 0770)
	internal.PanicOnError(err)

	cfg, err = internal.ReadConfigFromFile(cfgFile)
	cfg.FillInput()
	if err != nil {
		log.Fatalln(err)
	}

	log.Println("workDir = " + cfg.WorkingDirectory)

	context := internal.CreateContext(cfg)
	rena := internal.CreateRena(context)

	err = rena.CopyTo(dest, ignoreParserErrors)
	if err != nil {
		log.Panicln(err)
	}
}
