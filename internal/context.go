package internal

type Context struct {
	Config *Config
}

func CreateContext(config *Config) *Context {
	return &Context{
		Config: config,
	}
}
