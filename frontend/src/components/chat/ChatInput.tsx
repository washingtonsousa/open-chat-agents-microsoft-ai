"use client";

import { FormEvent, useRef } from "react";
import { Box, IconButton, Paper } from "@mui/material";
import SendIcon from "@mui/icons-material/Send";

interface Props {
  onSend: (message: string) => void;
  disabled?: boolean;
}

export function ChatInput({ onSend, disabled }: Props) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const value = textareaRef.current?.value.trim();
    if (!value) return;
    onSend(value);
    if (textareaRef.current) textareaRef.current.value = "";
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e as unknown as FormEvent);
    }
  }

  return (
    <Paper
      component="form"
      onSubmit={handleSubmit}
      elevation={3}
      sx={{ display: "flex", alignItems: "flex-end", gap: 1, p: 1.5, m: 2, mt: 0, borderRadius: 4 }}
    >
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
    </Paper>
  );
}
