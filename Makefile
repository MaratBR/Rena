RENA_BUILD_VERSION ?= 0.0.1

all:
	go build -o "./build/Rena_$(RENA_BUILD_VERSION)"