# AI extraction

AI is optional and **off by default** until the user enables it in Privacy settings.

Order of work: cheap rules → PDF text → OCR if needed → LLM only for missing fields.

LLM output must match a JSON schema (`isPurchase`, vendor, productName, dates, amount, currency, warranty months, serial, confidence). Invalid JSON is rejected. Dates and numbers are re-parsed in C#.

Untrusted source text is wrapped so the model cannot treat it as system instructions. Keepwise never uses customer data to train models. Provider adapters (`ILlmExtractor`, `IOcrProvider`) are replaceable; development uses no-op stubs when keys are absent.
