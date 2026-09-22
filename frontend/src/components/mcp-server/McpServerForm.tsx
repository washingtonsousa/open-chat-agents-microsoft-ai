"use client";

import { useState } from "react";
import { Alert, Box, Button, Stack, TextField, ToggleButton, ToggleButtonGroup, Typography } from "@mui/material";
import { mcpServerApi } from "@/services/api";
import type { McpAuthType, McpServer, McpServerCreate } from "@/types";

interface Props {
  onSaved: (server: McpServer) => void;
  onCancel: () => void;
  /** Quando presente, o form edita esse servidor em vez de criar um novo. */
  editing?: McpServer;
}

const DEFAULTS: McpServerCreate = {
  name: "",
  url: "",
  auth_type: "none",
  auth_header_name: "",
  secret: "",
};

export function McpServerForm({ onSaved, onCancel, editing }: Props) {
  const [form, setForm] = useState<McpServerCreate>(
    editing
      ? {
          name: editing.name,
          url: editing.url,
          auth_type: editing.auth_type,
          auth_header_name: editing.auth_header_name ?? "",
          secret: "",
        }
      : DEFAULTS
  );
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function set<K extends keyof McpServerCreate>(key: K, value: McpServerCreate[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const server = editing
        ? await mcpServerApi.update(editing.id, form)
        : await mcpServerApi.create(form);
      onSaved(server);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao salvar servidor MCP.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Box component="form" onSubmit={handleSubmit}>
      <Stack spacing={2.5}>
        <TextField
          label="Nome"
          required
          fullWidth
          value={form.name}
          onChange={(e) => set("name", e.target.value)}
          placeholder="Ex: GitHub"
        />

        <TextField
          label="URL"
          required
          fullWidth
          value={form.url}
          onChange={(e) => set("url", e.target.value)}
          placeholder="https://meu-servidor-mcp.exemplo.com/mcp"
        />

        <Box>
          <Typography variant="body2" sx={{ mb: 1, fontWeight: 500 }}>
            Autenticação
          </Typography>
          <ToggleButtonGroup
            fullWidth
            exclusive
            value={form.auth_type}
            onChange={(_, v: McpAuthType | null) => {
              if (!v) return;
              set("auth_type", v);
            }}
          >
            <ToggleButton value="none">Nenhuma</ToggleButton>
            <ToggleButton value="bearer-token">Bearer Token</ToggleButton>
            <ToggleButton value="header">Header customizado</ToggleButton>
          </ToggleButtonGroup>
        </Box>

        {form.auth_type === "header" && (
          <TextField
            label="Nome do header"
            required
            fullWidth
            value={form.auth_header_name ?? ""}
            onChange={(e) => set("auth_header_name", e.target.value)}
            placeholder="Ex: X-API-Key"
          />
        )}

        {form.auth_type !== "none" && (
          <TextField
            label="Token / segredo"
            type="password"
            fullWidth
            value={form.secret ?? ""}
            onChange={(e) => set("secret", e.target.value)}
            helperText={
              editing
                ? "Deixe em branco para manter o segredo já salvo."
                : "Guardado de forma criptografada — nunca é exibido de novo."
            }
          />
        )}

        {error && <Alert severity="error">{error}</Alert>}

        <Stack direction="row" spacing={1.5} sx={{ justifyContent: "flex-end" }}>
          <Button onClick={onCancel} color="inherit">
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={submitting}>
            {submitting ? "Salvando..." : editing ? "Salvar alterações" : "Adicionar servidor"}
          </Button>
        </Stack>
      </Stack>
    </Box>
  );
}
