# InkRoute AI Prompt Architecture

The OpenAI service no longer contains hard-coded prompt strings. `AiTattooPromptBuilder` composes generation and edit prompts from files under `AI/Prompts`.

- Core: rules shared by every request.
- Styles: one file per tattoo style with `Default.txt` fallback.
- Placements: one file per body placement with `Default.txt` fallback.
- Generation: main initial-generation template.
- Editing: edit template and operation-specific rules.

In Development prompt files are read on every request, so edits can be tested after restarting or while the application is running depending on the host file behavior. In non-Development environments they are cached.

No database migration is required for this refactor.
