"use client";

import { FormEvent, useRef, useState } from "react";
import { Box, IconButton, Paper, Tooltip } from "@mui/material";
import SendIcon from "@mui/icons-material/Send";
import ImageIcon from "@mui/icons-material/Image";
import CloseIcon from "@mui/icons-material/Close";
import type { ChatImageAttachment } from "@/services/api";

interface Props {
  onSend: (message: string, image?: ChatImageAttachment) => void;
  disabled?: boolean;
}

export function ChatInput({ onSend, disabled }: Props) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [image, setImage] = useState<ChatImageAttachment | null>(null);
  const [preview, setPreview] = useState<string | null>(null);

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const value = textareaRef.current?.value.trim();
    if (!value) return;
    onSend(value, image ?? undefined);
    if (textareaRef.current) textareaRef.current.value = "";
    clearImage();
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e as unknown as FormEvent);
    }
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = reader.result as string;
      const base64 = dataUrl.split(",")[1] ?? "";
      setImage({ base64, contentType: file.type });
      setPreview(dataUrl);
    };
    reader.readAsDataURL(file);
  }

  function clearImage() {
    setImage(null);
    setPreview(null);
  }

  return (
    <Paper
      component="form"
      onSubmit={handleSubmit}
      elevation={3}
      sx={{ display: "flex", flexDirection: "column", gap: 1, p: 1.5, m: 2, mt: 0, borderRadius: 4 }}
    >
      {preview && (
        <Box sx={{ position: "relative", width: 72, height: 72 }}>
          <Box
            component="img"
            src={preview}
            alt="Imagem anexada"
            sx={{ width: 72, height: 72, objectFit: "cover", borderRadius: 2, border: "1px solid", borderColor: "divider" }}
          />
          <IconButton
            size="small"
            onClick={clearImage}
            sx={{ position: "absolute", top: -8, right: -8, bgcolor: "grey.700", color: "white", "&:hover": { bgcolor: "grey.900" }, width: 20, height: 20 }}
          >
            <CloseIcon sx={{ fontSize: 12 }} />
          </IconButton>
        </Box>
      )}
      <Box sx={{ display: "flex", alignItems: "flex-end", gap: 1 }}>
        <input ref={fileInputRef} type="file" accept="image/*" hidden onChange={handleFileChange} />
        <Tooltip title="Anexar imagem">
          <span>
            <IconButton onClick={() => fileInputRef.current?.click()} disabled={disabled}>
              <ImageIcon fontSize="small" />
            </IconButton>
          </span>
        </Tooltip>
        <Box
          component="textarea"
          ref={textareaRef}
          rows={1}
          placeholder="Digite sua mensagem... (Enter para enviar)"
          disabled={disabled}
          onKeyDown={handleKeyDown}
          sx={{
            flex: 1,
            resize: "none",
            border: "none",
            outline: "none",
            bgcolor: "transparent",
            fontFamily: "inherit",
            fontSize: 14,
            px: 1,
            py: 1,
            maxHeight: 160,
            overflowY: "auto",
            "&:disabled": { opacity: 0.5 },
          }}
        />
        <IconButton type="submit" color="primary" disabled={disabled} sx={{ bgcolor: "primary.main", color: "white", "&:hover": { bgcolor: "primary.dark" }, "&.Mui-disabled": { bgcolor: "grey.300" } }}>
          <SendIcon fontSize="small" />
        </IconButton>
      </Box>
    </Paper>
  );
}
