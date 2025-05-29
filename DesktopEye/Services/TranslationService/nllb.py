from transformers import AutoTokenizer, AutoModelForSeq2SeqLM
import torch

# Load from local if needed:
# tokenizer = AutoTokenizer.from_pretrained("./local_model")
# model = AutoModelForSeq2SeqLM.from_pretrained("./local_model")

tokenizer = AutoTokenizer.from_pretrained("facebook/nllb-200-distilled-600M")
model = AutoModelForSeq2SeqLM.from_pretrained("facebook/nllb-200-distilled-600M")

def translate(text, src_lang="eng_Latn", tgt_lang="fra_Latn"):
    tokenizer.src_lang = src_lang
    encoded = tokenizer(text, return_tensors="pt")
    generated_tokens = model.generate(
        **encoded,
        forced_bos_token_id=tokenizer.convert_tokens_to_ids(tgt_lang)
    )
    return tokenizer.batch_decode(generated_tokens, skip_special_tokens=True)[0]

if __name__ == "__main__":
    text = "Hello, how are you?"
    translated_text = translate(text, src_lang="eng_Latn", tgt_lang="fra_Latn")
    print(f"Translated Text: {translated_text}")
    