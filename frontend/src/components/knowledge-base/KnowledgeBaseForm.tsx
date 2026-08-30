"use client";

import { useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  MenuItem,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import { knowledgeBaseApi, modelsApi } from "@/services/api";
import type { KnowledgeBase, KnowledgeBaseCreate, LLMProvider, ModelInfo } from "@/types";

interface Props {
  onCreated: (kb: KnowledgeBase) => void;
  onCancel: () => void;
}

const DEFAULTS: KnowledgeBaseCreate = {
  name: "",
  description: "",
  chunk_size: 1000,
  chunk_overlap: 200,
  embedding_provider: "ollama",
  embedding_model: "",
};

export function KnowledgeBaseForm({ onCreated, onCancel }: Props) {
  const [form, setForm] = useState<KnowledgeBaseCreate>(DEFAULTS);
  const [models, setModels] = useState<ModelInfo[]>([]);
  const [loadingModels, setLoadingModels] = useState(true);
  const [modelsError, setModelsError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function fetchModels(provider: LLMProvider) {
    setLoadingModels(true);
    setModelsError(null);
    setModels([]);
    set("embedding_model", "");
    try {
      const res = provider === "bedrock" ? await modelsApi.listBedrock() : await modelsApi.listOllama();
      setModels(res.models);
      if (res.models.length > 0) set("embedding_model", res.models[0].name);
      if (res.models.length === 0) setModelsError("Nenhum modelo encontrado.");
    } catch {
      setModelsError("Não foi possível listar os modelos.");
    } finally {
      setLoadingModels(false);
    }
  }

  useEffect(() => {
    fetchModels("ollama");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const kb = await knowledgeBaseApi.create(form);
      onCreated(kb);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao criar base de conhecimento.");
    } finally {
      setSubmitting(false);
    }
  }

  function set<K extends keyof KnowledgeBaseCreate>(key: K, value: KnowledgeBaseCreate[K]) {
    setForm((f) => ({ ...f, [key]: value }));
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
          placeholder="Ex: Políticas internas"
        />

        <TextField
          label="Descrição"
          fullWidth
          multiline
          rows={2}
          value={form.description}
          onChange={(e) => set("description", e.target.value)}
        />

        <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2.5 }}>
          <TextField
            label="Tamanho do chunk (caracteres)"
            type="number"
            fullWidth
            value={form.chunk_size}
            onChange={(e) => set("chunk_size", parseInt(e.target.value) || 0)}
            slotProps={{ htmlInput: { min: 100 } }}
          />
          <TextField
            label="Sobreposição (overlap)"
            type="number"
            fullWidth
            value={form.chunk_overlap}
            onChange={(e) => set("chunk_overlap", parseInt(e.target.value) || 0)}
            slotProps={{ htmlInput: { min: 0 } }}
          />
        </Box>

        <Box>
          <Typography variant="body2" sx={{ mb: 1, fontWeight: 500 }}>
            Provedor do modelo de embedding
          </Typography>
          <ToggleButtonGroup
            fullWidth
            exclusive
            value={form.embedding_provider}
            onChange={(_, v) => {
              if (!v) return;
              set("embedding_provider", v);
              fetchModels(v);
            }}
          >
            <ToggleButton value="ollama">Ollama (local)</ToggleButton>
            <ToggleButton value="bedrock">AWS Bedrock</ToggleButton>
          </ToggleButtonGroup>
        </Box>

        {modelsError ? (
          <Alert severity="warning">{modelsError}</Alert>
        ) : (
          <TextField
            select
            label="Modelo de embedding"
            required
            fullWidth
            value={form.embedding_model}
            onChange={(e) => set("embedding_model", e.target.value)}
            disabled={loadingModels}
            helperText={
              loadingModels
                ? "Carregando modelos..."
                : <>Use um modelo de embedding (ex: <code>nomic-embed-text</code>), não um modelo de chat.</>
            }
          >
            {models.map((m) => (
              <MenuItem key={m.name} value={m.name}>
                {m.name}
              </MenuItem>
            ))}
          </TextField>
        )}

        {error && <Alert severity="error">{error}</Alert>}

        <Stack direction="row" spacing={1.5} sx={{ justifyContent: "flex-end" }}>
          <Button onClick={onCancel} color="inherit">
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={submitting || loadingModels || !form.embedding_model}>
            {submitting ? "Criando..." : "Criar base de conhecimento"}
          </Button>
        </Stack>
      </Stack>
    </Box>
  );
}
