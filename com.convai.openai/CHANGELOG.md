# Changelog

## 1.0.0 - Initial release

## 1.1.0 - 2025.11.13

- Adding assembly definitions for open ai library.
- Fixing samples for open ai conv ai
- Fixing tools sample

## 1.2.0 - 2026.10.26

- Support OpenAI Realtime GA schema (`gpt-realtime`, `gpt-realtime-mini`):
  - Added required `session.type = "realtime"` parameter.
  - Omitted legacy top-level `modalities` and `voice` fields on GA sessions.
  - Replaced flat audio properties with structured `session.audio.input` (PCM 24kHz, server VAD, whisper-1) and `session.audio.output` (PCM 24kHz, voice).
- Added backward-compatible dual schema support for legacy beta/preview models (`gpt-4o-realtime-preview`).
- Conditionally apply `OpenAI-Beta: realtime=v1` handshake header only for preview models.
- Added event listeners for GA server events: `response.output_audio.delta`, `response.output_audio.done`, `response.output_audio_transcript.delta`, `response.output_audio_transcript.done`, and `conversation.item.added`.
