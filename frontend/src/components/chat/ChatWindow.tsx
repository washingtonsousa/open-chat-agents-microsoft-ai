"use client";

import { useEffect, useRef, useState } from "react";
import { Alert, Avatar, Box, Paper, Typography } from "@mui/material";
import SmartToyIcon from "@mui/icons-material/SmartToy";
import { chatApi, type ChatImageAttachment } from "@/services/api";
import type { Message } from "@/types";
import { ChatInput } from "./ChatInput";
import { MessageBubble } from "./MessageBubble";

interface Props {
  sessionId: string;
}

export function ChatWindow({ sessionId }: Props) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [streamingContent, setStreamingContent] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setMessages([]);
    setStreamingContent(null);
    setError(null);
    chatApi
      .history(sessionId)
      .then((res) => setMessages(res.messages))
      .catch(() => setError("Não foi possível carregar o histórico."));
  }, [sessionId]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, streamingContent]);

  async function handleSend(text: string, image?: ChatImageAttachment) {
    setLoading(true);
    setError(null);
    setStreamingContent("");

    try {
      await chatApi.stream(
        sessionId,
        text,
        {
          onUserMessage: (msg) =>
            setMessages((prev) => [...prev, msg]),
          onChunk: (chunk) =>
            setStreamingContent((prev) => (prev ?? "") + chunk),
          onDone: (msg) => {
            setMessages((prev) => [...prev, msg]);
            setStreamingContent(null);
          },
        },
        image
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao enviar mensagem.");
      setStreamingContent(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <Box sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
      <Box sx={{ flex: 1, overflowY: "auto", px: 3, py: 3, display: "flex", flexDirection: "column", gap: 1.5 }}>
        {messages.length === 0 && !loading && (
          <Typography align="center" color="text.secondary" variant="body2" sx={{ mt: 8 }}>
            Nenhuma mensagem ainda. Diga olá!
          </Typography>
        )}
        {messages.map((m) => (
          <MessageBubble key={m.id} message={m} />
        ))}
        {streamingContent !== null && (
          <Box sx={{ display: "flex", justifyContent: "flex-start", gap: 1, alignItems: "flex-end" }}>
            <Avatar sx={{ width: 28, height: 28, bgcolor: "primary.main" }}>
              <SmartToyIcon sx={{ fontSize: 16 }} />
            </Avatar>
            <Paper elevation={2} sx={{ maxWidth: "75%", px: 2, py: 1.25, borderRadius: 3, borderBottomLeftRadius: 4 }}>
              <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
                {streamingContent}
                <Box
                  component="span"
                  sx={{
                    display: "inline-block",
                    width: 6,
                    height: 14,
                    ml: 0.5,
                    bgcolor: "grey.400",
                    verticalAlign: "middle",
                    animation: "blink 1s step-start infinite",
                    "@keyframes blink": { "50%": { opacity: 0 } },
                  }}
                />
              </Typography>
            </Paper>
          </Box>
        )}
        {error && <Alert severity="error">{error}</Alert>}
        <div ref={bottomRef} />
      </Box>
      <ChatInput onSend={handleSend} disabled={loading} />
    </Box>
  );
}
