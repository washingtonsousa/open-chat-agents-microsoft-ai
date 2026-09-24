"use client";

import { useEffect, useState } from "react";
import { Avatar, Box, IconButton, Paper, Tooltip, Typography } from "@mui/material";
import PersonIcon from "@mui/icons-material/Person";
import SmartToyIcon from "@mui/icons-material/SmartToy";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import CheckIcon from "@mui/icons-material/Check";
import ReactMarkdown from "react-markdown";
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter";
import { oneDark } from "react-syntax-highlighter/dist/esm/styles/prism";
import remarkGfm from "remark-gfm";
import { authHeaders, BASE_URL } from "@/services/api";
import type { Message } from "@/types";

interface Props {
  message: Message;
}

/**
 * A imagem de uma mensagem é servida atrás de autenticação, então não dá pra usar
 * <img src> puro (não manda header de Authorization) — busca via fetch e vira blob URL.
 */
function AuthenticatedImage({ path }: { path: string }) {
  const [blobUrl, setBlobUrl] = useState<string | null>(null);

  useEffect(() => {
    let objectUrl: string | null = null;
    let cancelled = false;

    fetch(`${BASE_URL}/api/v1${path}`, { headers: authHeaders() })
      .then((res) => (res.ok ? res.blob() : Promise.reject(new Error("Falha ao carregar imagem"))))
      .then((blob) => {
        if (cancelled) return;
        objectUrl = URL.createObjectURL(blob);
        setBlobUrl(objectUrl);
      })
      .catch(() => {});

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [path]);

  if (!blobUrl) return null;

  return (
    <Box
      component="img"
      src={blobUrl}
      alt="Imagem anexada"
      sx={{ maxWidth: "100%", maxHeight: 280, borderRadius: 2, display: "block", mb: 1 }}
    />
  );
}

function CopyButton({ code }: { code: string }) {
  const [copied, setCopied] = useState(false);

  function handleCopy() {
    navigator.clipboard.writeText(code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <Tooltip title={copied ? "Copiado!" : "Copiar código"}>
      <IconButton
        size="small"
        onClick={handleCopy}
        sx={{ position: "absolute", top: 6, right: 6, color: "grey.300", bgcolor: "grey.800", "&:hover": { bgcolor: "grey.700" } }}
      >
        {copied ? <CheckIcon sx={{ fontSize: 14 }} /> : <ContentCopyIcon sx={{ fontSize: 14 }} />}
      </IconButton>
    </Tooltip>
  );
}

export function MessageBubble({ message }: Props) {
  const isUser = message.role === "user";

  return (
    <Box sx={{ display: "flex", justifyContent: isUser ? "flex-end" : "flex-start", gap: 1, alignItems: "flex-end" }}>
      {!isUser && (
        <Avatar sx={{ width: 28, height: 28, bgcolor: "primary.main" }}>
          <SmartToyIcon sx={{ fontSize: 16 }} />
        </Avatar>
      )}
      <Paper
        elevation={2}
        sx={{
          maxWidth: "75%",
          px: 2,
          py: 1.25,
          borderRadius: 3,
          ...(isUser
            ? { bgcolor: "primary.main", color: "primary.contrastText", borderBottomRightRadius: 4 }
            : { bgcolor: "background.paper", borderBottomLeftRadius: 4 }),
        }}
      >
        {message.image_url && <AuthenticatedImage path={message.image_url} />}
        {isUser ? (
          <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
            {message.content}
          </Typography>
        ) : (
          <Box sx={{ fontSize: 14, lineHeight: 1.6 }}>
            <ReactMarkdown
              remarkPlugins={[remarkGfm]}
              components={{
                code({ className, children, ...props }) {
                  const match = /language-(\w+)/.exec(className ?? "");
                  const code = String(children).replace(/\n$/, "");
                  const isBlock = !!match || code.includes("\n");

                  if (isBlock) {
                    return (
                      <Box sx={{ position: "relative", my: 1, borderRadius: 2, overflow: "hidden", fontSize: 12 }}>
                        <CopyButton code={code} />
                        <SyntaxHighlighter
                          style={oneDark}
                          language={match?.[1] ?? "text"}
                          PreTag="div"
                          customStyle={{ margin: 0, borderRadius: "0.5rem", paddingTop: "2rem" }}
                        >
                          {code}
                        </SyntaxHighlighter>
                      </Box>
                    );
                  }

                  return (
                    <Box
                      component="code"
                      sx={{ bgcolor: "grey.200", px: 0.75, py: 0.25, borderRadius: 1, fontSize: 12, fontFamily: "monospace" }}
                      {...props}
                    >
                      {children}
                    </Box>
                  );
                },
                p({ children }) {
                  return (
                    <Typography variant="body2" component="p" sx={{ mb: 1, "&:last-child": { mb: 0 } }}>
                      {children}
                    </Typography>
                  );
                },
                ul({ children }) {
                  return <Box component="ul" sx={{ listStyle: "disc", pl: 2.5, mb: 1 }}>{children}</Box>;
                },
                ol({ children }) {
                  return <Box component="ol" sx={{ listStyle: "decimal", pl: 2.5, mb: 1 }}>{children}</Box>;
                },
                strong({ children }) {
                  return <Box component="strong" sx={{ fontWeight: 600 }}>{children}</Box>;
                },
                a({ href, children }) {
                  return (
                    <a href={href} target="_blank" rel="noopener noreferrer" style={{ color: "inherit" }}>
                      {children}
                    </a>
                  );
                },
              }}
            >
              {message.content}
            </ReactMarkdown>
          </Box>
        )}
      </Paper>
      {isUser && (
        <Avatar sx={{ width: 28, height: 28, bgcolor: "grey.400" }}>
          <PersonIcon sx={{ fontSize: 16 }} />
        </Avatar>
      )}
    </Box>
  );
}
