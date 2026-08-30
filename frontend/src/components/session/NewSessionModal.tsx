"use client";

import { useState } from "react";
import {
  Alert,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Stack,
  TextField,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import type { Agent, Session } from "@/types";
import { sessionApi } from "@/services/api";

interface Props {
  agents: Agent[];
  onCreated: (session: Session) => void;
  onClose: () => void;
}

export function NewSessionModal({ agents, onCreated, onClose }: Props) {
  const [title, setTitle] = useState("Nova conversa");
  const [selectedAgentId, setSelectedAgentId] = useState<string>(agents[0]?.id ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const session = await sessionApi.create(title, selectedAgentId || null);
      onCreated(session);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao criar conversa.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        Nova conversa
        <IconButton onClick={onClose} size="small">
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <Stack component="form" onSubmit={handleSubmit} spacing={2.5} sx={{ pt: 0.5 }}>
          <TextField
            label="Título"
            fullWidth
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />

          {agents.length === 0 ? (
            <Alert severity="warning">Nenhum agente criado. Crie um agente primeiro.</Alert>
          ) : (
            <TextField
              select
              label="Agente"
              fullWidth
              value={selectedAgentId}
              onChange={(e) => setSelectedAgentId(e.target.value)}
            >
              {agents.map((a) => (
                <MenuItem key={a.id} value={a.id}>
                  {a.name} — {a.llm_model}
                </MenuItem>
              ))}
            </TextField>
          )}

          {error && <Alert severity="error">{error}</Alert>}

          <Stack direction="row" spacing={1.5} sx={{ justifyContent: "flex-end" }}>
            <Button onClick={onClose} color="inherit">
              Cancelar
            </Button>
            <Button type="submit" variant="contained" disabled={submitting || agents.length === 0}>
              {submitting ? "Criando..." : "Iniciar conversa"}
            </Button>
          </Stack>
        </Stack>
      </DialogContent>
    </Dialog>
  );
}
