"use client";

import { useEffect, useRef, useState } from "react";
import { chatApi } from "@/services/api";
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

  async function handleSend(text: string) {
    setLoading(true);
    setError(null);
    setStreamingContent("");

    try {
      await chatApi.stream(sessionId, text, {
        onUserMessage: (msg) =>
          setMessages((prev) => [...prev, msg]),
        onChunk: (chunk) =>
          setStreamingContent((prev) => (prev ?? "") + chunk),
        onDone: (msg) => {
          setMessages((prev) => [...prev, msg]);
          setStreamingContent(null);
        },
      });
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao enviar mensagem.");
      setStreamingContent(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex-1 overflow-y-auto px-6 py-4 space-y-3">
        {messages.length === 0 && !loading && (
          <p className="text-center text-gray-400 text-sm mt-10">
            Nenhuma mensagem ainda. Diga olá!
          </p>
        )}
        {messages.map((m) => (
          <MessageBubble key={m.id} message={m} />
        ))}
        {streamingContent !== null && (
          <div className="flex justify-start">
            <div className="max-w-[75%] rounded-2xl rounded-bl-sm px-4 py-3 text-sm leading-relaxed whitespace-pre-wrap bg-gray-100 text-gray-800">
              {streamingContent}
              <span className="inline-block w-2 h-4 ml-0.5 bg-gray-400 animate-pulse align-middle" />
            </div>
          </div>
        )}
        {error && (
          <p className="text-center text-red-500 text-sm">{error}</p>
        )}
        <div ref={bottomRef} />
      </div>
      <ChatInput onSend={handleSend} disabled={loading} />
    </div>
  );
}
