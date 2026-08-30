"use client";

import { useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  MenuItem,
  Slider,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import { agentApi, modelsApi } from "@/services/api";
import { KbMultiSelect } from "@/components/knowledge-base/KbMultiSelect";
import type { Agent, AgentCreate, LLMProvider, ModelInfo } from "@/types";

interface Props {
  agent?: Agent;
  onSaved: (agent: Agent) => void;
  onCancel: () => void;
}

const DEFAULTS: AgentCreate = {
  name: "",
  provider: "ollama",
  llm_model: "",
  temperature: 0.7,
  max_tokens: null,
  system_prompt: "Você é um assistente prestativo e amigável.",
  knowledge_base_ids: [],
};

export function AgentForm({ agent, onSaved, onCancel }: Props) {
  const isEditing = !!agent;
  const [form, setForm] = useState<AgentCreate>(
    agent
      ? {
          name: agent.name,
          provider: agent.provider,
          llm_model: agent.llm_model,
          temperature: agent.temperature,
          max_tokens: agent.max_tokens,
          system_prompt: agent.system_prompt,
          knowledge_base_ids: agent.knowledge_bases.map((kb) => kb.id),
        }
      : DEFAULTS
  );
  const [models, setModels] = useState<ModelInfo[]>([]);
  const [loadingModels, setLoadingModels] = useState(true);
  const [modelsError, setModelsError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function fetchModels(provider: LLMProvider, keepModel = false) {
    setLoadingModels(true);
    setModelsError(null);
    setModels([]);
    if (!keepModel) set("llm_model", "");
    try {
      const res = provider === "bedrock"
        ? await modelsApi.listBedrock()
        : await modelsApi.listOllama();
      setModels(res.models);
      if (!keepModel && res.models.length > 0) set("llm_model", res.models[0].name);
      if (res.models.length === 0) setModelsError(
        provider === "bedrock"
          ? "Nenhum modelo encontrado no Bedrock. Verifique as credenciais AWS e a região configurada."
          : "Nenhum modelo encontrado no Ollama. Verifique se o serviço está rodando."
      );
    } catch {
      setModelsError(
        provider === "bedrock"
          ? "Não foi possível listar modelos do Bedrock. Verifique as credenciais AWS."
          : "Não foi possível conectar ao Ollama."
      );
    } finally {
      setLoadingModels(false);
    }
  }

  useEffect(() => {
    fetchModels(form.provider, isEditing);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function handleProviderChange(provider: LLMProvider | null) {
    if (!provider) return;
    set("provider", provider);
    fetchModels(provider);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const saved = isEditing ? await agentApi.update(agent!.id, form) : await agentApi.create(form);
      onSaved(saved);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao salvar agente.");
    } finally {
      setSubmitting(false);
    }
  }

  function set<K extends keyof AgentCreate>(key: K, value: AgentCreate[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  return (
    <Box component="form" onSubmit={handleSubmit}>
      <Stack spacing={2.5}>
        <TextField
          label="Nome do agente"
          required
          fullWidth
          value={form.name}
          onChange={(e) => set("name", e.target.value)}
          placeholder="Ex: Assistente de Vendas"
        />

        <Box>
          <Typography variant="body2" sx={{ mb: 1, fontWeight: 500 }}>
            Provedor
          </Typography>
          <ToggleButtonGroup
            fullWidth
            exclusive
            value={form.provider}
            onChange={(_, v) => handleProviderChange(v)}
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
            label="Modelo LLM"
            required
            fullWidth
            value={form.llm_model}
            onChange={(e) => set("llm_model", e.target.value)}
            disabled={loadingModels}
            helperText={loadingModels ? "Carregando modelos..." : undefined}
          >
            {form.llm_model && !models.some((m) => m.name === form.llm_model) && (
              <MenuItem value={form.llm_model}>{form.llm_model}</MenuItem>
            )}
            {models.map((m) => (
              <MenuItem key={m.name} value={m.name}>
                {m.name}{m.provider ? ` — ${m.provider}` : ""}
              </MenuItem>
            ))}
          </TextField>
        )}

        <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2.5 }}>
          <Box>
            <Typography variant="body2" sx={{ fontWeight: 500 }}>
              Temperatura ({form.temperature})
            </Typography>
            <Slider
              min={0}
              max={2}
              step={0.1}
              value={form.temperature}
              onChange={(_, v) => set("temperature", v as number)}
              marks={[
                { value: 0, label: "Preciso" },
                { value: 2, label: "Criativo" },
              ]}
              sx={{ mt: 1 }}
            />
          </Box>

          <TextField
            label="Máx. tokens"
            type="number"
            fullWidth
            value={form.max_tokens ?? ""}
            onChange={(e) => set("max_tokens", e.target.value ? parseInt(e.target.value) : null)}
            placeholder="Sem limite"
            slotProps={{ htmlInput: { min: 1 } }}
          />
        </Box>

        <TextField
          label="Prompt inicial (system prompt)"
          required
          fullWidth
          multiline
          rows={4}
          value={form.system_prompt}
          onChange={(e) => set("system_prompt", e.target.value)}
          placeholder="Defina o comportamento e contexto do agente..."
        />

        <Box>
          <Typography variant="body2" sx={{ mb: 1, fontWeight: 500 }}>
            Bases de conhecimento (RAG)
          </Typography>
          <KbMultiSelect value={form.knowledge_base_ids} onChange={(ids) => set("knowledge_base_ids", ids)} />
        </Box>

        {error && <Alert severity="error">{error}</Alert>}

        <Stack direction="row" spacing={1.5} sx={{ justifyContent: "flex-end" }}>
          <Button onClick={onCancel} color="inherit">
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={submitting || loadingModels || !form.llm_model}>
            {submitting ? "Salvando..." : isEditing ? "Salvar alterações" : "Criar agente"}
          </Button>
        </Stack>
      </Stack>
    </Box>
  );
}
