"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  Alert,
  Avatar,
  Box,
  Chip,
  IconButton,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import DeleteIcon from "@mui/icons-material/Delete";
import CircularProgress from "@mui/material/CircularProgress";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import CancelIcon from "@mui/icons-material/Cancel";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import DescriptionIcon from "@mui/icons-material/Description";
import { knowledgeBaseApi } from "@/services/api";
import type { KbDocument, KnowledgeBase } from "@/types";

interface Props {
  kb: KnowledgeBase;
  onDeleted: () => void;
}

const STATUS_LABEL: Record<KbDocument["status"], string> = {
  uploaded: "Aguardando processamento",
  processing: "Processando",
  completed: "Concluído",
  failed: "Falhou",
};

function StatusIcon({ status }: { status: KbDocument["status"] }) {
  switch (status) {
    case "uploaded":
      return <HourglassEmptyIcon fontSize="small" color="disabled" />;
    case "processing":
      return <CircularProgress size={18} />;
    case "completed":
      return <CheckCircleIcon fontSize="small" color="success" />;
    case "failed":
      return <CancelIcon fontSize="small" color="error" />;
  }
}

export function KbDetailPanel({ kb, onDeleted }: Props) {
  const [documents, setDocuments] = useState<KbDocument[]>([]);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const loadDocuments = useCallback(async () => {
    const res = await knowledgeBaseApi.listDocuments(kb.id);
    setDocuments(res.documents);
  }, [kb.id]);

  useEffect(() => {
    loadDocuments();
  }, [loadDocuments]);

  useEffect(() => {
    const hasPending = documents.some((d) => d.status === "uploaded" || d.status === "processing");
    if (!hasPending) return;
    const interval = setInterval(loadDocuments, 2000);
    return () => clearInterval(interval);
  }, [documents, loadDocuments]);

  async function handleFiles(files: FileList | null) {
    if (!files || files.length === 0) return;
    setUploading(true);
    setError(null);
    try {
      for (const file of Array.from(files)) {
        const doc = await knowledgeBaseApi.uploadDocument(kb.id, file);
        setDocuments((prev) => [...prev, doc]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao enviar arquivo.");
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  async function handleDelete() {
    if (!confirm(`Excluir a base de conhecimento "${kb.name}"? Isso remove todos os documentos e vetores.`)) return;
    await knowledgeBaseApi.delete(kb.id);
    onDeleted();
  }

  return (
    <Box sx={{ flex: 1, overflowY: "auto", p: 4 }}>
      <Stack direction="row" sx={{ mb: 3, justifyContent: "space-between", alignItems: "flex-start" }}>
        <Box>
          <Typography variant="h5" sx={{ fontWeight: 600 }}>
            {kb.name}
          </Typography>
          {kb.description && (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {kb.description}
            </Typography>
          )}
          <Stack direction="row" spacing={1} sx={{ mt: 1.5, gap: 1, flexWrap: "wrap" }}>
            <Chip size="small" variant="outlined" label={`Chunk: ${kb.chunk_size} chars (overlap ${kb.chunk_overlap})`} />
            <Chip size="small" variant="outlined" label={`Embedding: ${kb.embedding_model} (${kb.embedding_provider})`} />
            <Chip size="small" variant="outlined" label={`Dimensões: ${kb.embedding_dimensions}`} />
            {kb.created_by && <Chip size="small" variant="outlined" label={`Criado por: ${kb.created_by.username}`} />}
          </Stack>
        </Box>
        <IconButton onClick={handleDelete} color="error" title="Excluir base de conhecimento">
          <DeleteIcon />
        </IconButton>
      </Stack>

      <Box sx={{ mb: 4 }}>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept=".pdf,.txt,.md,.docx"
          hidden
          onChange={(e) => handleFiles(e.target.files)}
        />
        <Paper
          variant="outlined"
          onClick={() => fileInputRef.current?.click()}
          sx={{
            p: 3,
            borderStyle: "dashed",
            borderWidth: 2,
            textAlign: "center",
            cursor: uploading ? "default" : "pointer",
            opacity: uploading ? 0.6 : 1,
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            gap: 1,
            "&:hover": { borderColor: "primary.main", bgcolor: "action.hover" },
          }}
        >
          <UploadFileIcon color="primary" />
          <Typography variant="body2" color="text.secondary">
            {uploading ? "Enviando..." : "Enviar arquivos (.pdf, .docx, .txt, .md)"}
          </Typography>
        </Paper>
        {error && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {error}
          </Alert>
        )}
      </Box>

      <Typography variant="overline" color="text.secondary">
        Documentos ({documents.length})
      </Typography>
      {documents.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          Nenhum documento enviado ainda.
        </Typography>
      ) : (
        <List sx={{ mt: 0.5 }}>
          {documents.map((doc) => (
            <Paper key={doc.id} variant="outlined" sx={{ mb: 1 }}>
              <ListItem>
                <ListItemAvatar sx={{ minWidth: 44 }}>
                  <Avatar sx={{ width: 32, height: 32, bgcolor: "grey.100" }}>
                    <DescriptionIcon fontSize="small" color="action" />
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary={doc.file_name}
                  secondary={
                    STATUS_LABEL[doc.status] +
                    (doc.status === "completed" ? ` — ${doc.chunk_count} chunks` : "") +
                    (doc.status === "failed" && doc.error_message ? `: ${doc.error_message}` : "")
                  }
                />
                <StatusIcon status={doc.status} />
              </ListItem>
            </Paper>
          ))}
        </List>
      )}
    </Box>
  );
}
