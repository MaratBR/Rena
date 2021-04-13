package internal

import (
	"errors"
	"fmt"
	"io/fs"
	"log"
	"os"
	"path/filepath"
	"strings"
	"text/template"
	"time"
)

type Rena struct {
	Context *Context
	time    time.Time
}

func (rena *Rena) startTimer() {
	rena.time = time.Now()
}

func (rena *Rena) stopTimer() int64 {
	now := time.Now()
	return now.Sub(rena.time).Microseconds()
}

func (rena Rena) isExcluded(path string) bool {
	for _, pattern := range rena.Context.Config.Exclude {
		if match, _ := filepath.Match(pattern, path); match {
			return true
		}
	}
	return false
}

func (rena Rena) copyFile(src string, dest string) error {
	newFile, err := os.Create(dest)

	if err != nil {
		return err
	}

	defer mustCloseFile(newFile)

	var content string
	{
		var data []byte
		data, err = os.ReadFile(src)

		if err != nil {
			return err
		}

		content = string(data)
	}

	content, err = rena.expand(content)
	newFile.Write([]byte(content))

	if err != nil {
		return err
	}

	return nil
}

func (rena Rena) expand(text string) (string, error) {
	tpl, err := template.New("noname").Parse(text)
	if err != nil {
		return "", err
	}

	buf := strings.Builder{}
	err = tpl.Execute(&buf, map[string]interface{}{
		"context": rena.Context,
		"config":  rena.Context.Config,
		"params":  rena.Context.Config.Parameters,
	})
	if err != nil {
		return "", err
	}
	return buf.String(), nil
}

// stripWorkDir removes workDir from beginning of provided path if it's there.
// Returns string without '/' at the beginning. Will cause panic if called with workDir value.
func (rena Rena) stripWorkDir(path string) string {
	if strings.HasPrefix(path, rena.Context.Config.WorkingDirectory) {
		path = path[len(rena.Context.Config.WorkingDirectory)+1:]
	}
	return path
}

func (rena Rena) CopyTo(dest string) error {
	_ = os.Mkdir(dest, os.ModeDir)
	workDir := rena.Context.Config.WorkingDirectory
	return filepath.Walk(workDir, func(path string, info fs.FileInfo, err error) error {
		if path == workDir {
			return nil
		}

		pathNoWorkDir := rena.stripWorkDir(path)
		if excluded := rena.isExcluded(pathNoWorkDir); excluded {
			log.Println("excluded: " + path)
			if info.IsDir() {
				return filepath.SkipDir
			} else {
				return errors.New("excluded: " + path)
			}
		}

		baseName, fileName := filepath.Split(pathNoWorkDir)

		fileName, err = rena.expand(fileName)
		if err != nil {
			return err
		}

		newBaseName := filepath.Join(dest, baseName)
		newPath := filepath.Join(newBaseName, fileName)

		if info.IsDir() {
			log.Println("mkdir: " + newPath)
			return os.Mkdir(newPath, 0770)
		} else {
			rena.startTimer()
			err = rena.copyFile(path, newPath)
			if err == nil {
				log.Println(fmt.Sprint(rena.stopTimer()) + "us " + newPath)
			}
			return err
		}
	})
}

func CreateRena(context *Context) *Rena {
	rena := &Rena{Context: context}
	return rena
}
