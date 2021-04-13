package reno

type Config {
	Root string `yaml:"root"`
	Variables map[string]string `yaml:"variables"`
}

func ReadConfigFromFile(filename string) {
	f, err := os.Open();
}