"use client";

import { useState } from "react";
import { Avatar, Box, IconButton, Paper, Tooltip, Typography } from "@mui/material";
import PersonIcon from "@mui/icons-material/Person";
import SmartToyIcon from "@mui/icons-material/SmartToy";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import CheckIcon from "@mui/icons-material/Check";
import ReactMarkdown from "react-markdown";
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter";
import { oneDark } from "react-syntax-highlighter/dist/esm/styles/prism";
import remarkGfm from "remark-gfm";
import type { Message } from "@/types";

interface Props {
  message: Message;
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
