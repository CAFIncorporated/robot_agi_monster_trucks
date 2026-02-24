group "default" {
  targets = ["prod", "test"]
}

target "prod" {
  dockerfile = "docker/prod.Dockerfile"
  context    = "."
  tags       = ["coordinate-service:latest", "coordinate-service:prod"]
}

target "test" {
  dockerfile = "docker/test.Dockerfile"
  context    = "."
  tags       = ["coordinate-service:test"]
}
