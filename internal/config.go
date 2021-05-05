package internal

import (
	"bufio"
	"log"
	"os"
	"path/filepath"
	"regexp"
	"strings"

	"gopkg.in/yaml.v2"
)

var (
	inputRegex                = regexp.MustCompile(`^{(?:input|>)(:.*)?}$`)
	scanner    *bufio.Scanner = bufio.NewScanner(os.Stdin)
)

func getScanner() *bufio.Scanner {
	if scanner == nil {
		scanner = bufio.NewScanner(os.Stdin)
	}
	return scanner
}

type Config struct {
	Parameters       map[string]interface{} `yaml:"parameters"`
	IgnoreDotGit     bool                   `yaml:"ignoreGit"`
	Name             string                 `yaml:"name"`
	Exclude          []string               `yaml:"exclude"`
	WorkingDirectory string                 `yaml:"workDir"`
}

func (config *Config) FillInput() {
	if config.Name == "" {
		config.Name = os.Getenv("RENA_NAME")
	}

	for k, v := range config.Parameters {
		if strv, ok := v.(string); ok {
			match := inputRegex.FindStringSubmatch(strv)
			if len(match) == 2 {
				match[1] = strings.Trim(match[1], " \t")
				if match[1] == "" || match[1] == ":" {
					match[1] = k
				}

				print("please input '" + match[1][1:] + "'>")
				if !getScanner().Scan() {
					log.Fatalln("Failed to read from stdin")
				}
				config.Parameters[k] = getScanner().Text()
			}
		}
	}
}

func defaultConfig() *Config {
	return &Config{
		IgnoreDotGit: true,
	}
}

func ReadConfigFromFile(path string) (*Config, error) {
	f, err := os.Open(path)
	if err != nil {
		return nil, err
	}

	decoder := yaml.NewDecoder(f)
	cfg := defaultConfig()
	err = decoder.Decode(cfg)
	if err != nil {
		return nil, err
	}

	if cfg.WorkingDirectory == "" {
		cfg.WorkingDirectory = filepath.Dir(path)
	} else if !filepath.IsAbs(cfg.WorkingDirectory) {
		cfg.WorkingDirectory = filepath.Join(filepath.Dir(path), cfg.WorkingDirectory)
	}

	return cfg, nil
}
