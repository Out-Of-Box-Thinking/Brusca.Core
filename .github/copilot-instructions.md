# Copilot Instructions

## Project Guidelines
- In Brusca, original source files must remain READ-ONLY at all times. PII redaction, document classification, and structure-execution moves must never mutate originals — all materialized output goes to the AlternateExecutionPath as copies. Claude must only ever see redacted content + DocumentType/Extension counts; raw PII and original filenames must never leave the host.