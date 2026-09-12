output "function_url" {
  description = "Public HTTPS endpoint for the API -- set this as EXPO_PUBLIC_API_BASE_URL when building the app."
  value       = aws_lambda_function_url.api.function_url
}
