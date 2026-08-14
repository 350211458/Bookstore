# Command: /check-arch
Please inspect the codebase in `src/` to ensure:
1. Every service follows Clean Architecture.
2. No service directly references another service's private classes.
3. All inter-service communications use gRPC or Refit HTTP clients.
