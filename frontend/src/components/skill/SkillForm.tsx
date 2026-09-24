"use client";

import { useState } from "react";
import { Alert, Box, Button, Paper, Stack, Tab, Tabs, TextField, Typography } from "@mui/material";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { skillApi } from "@/services/api";
import type { Skill, SkillCreate } from "@/types";

interface Props {
  onSaved: (skill: Skill) => void;
  onCancel: () => void;
  editing?: Skill;
}

const DEFAULTS: SkillCreate = { name: "", description: "", content: "" };

export function SkillForm({ onSaved, onCancel, editing }: Props) {
  const [form, setForm] = useState<SkillCreate>(
    editing ? { name: editing.name, description: editing.description, content: editing.content } : DEFAULTS
  );
  const [tab, setTab] = useState<"edit" | "preview">("edit");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function set<K extends keyof SkillCreate>(key: K, value: SkillCreate[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const skill = editing ? await skillApi.update(editing.id, form) : await skillApi.create(form);
      onSaved(skill);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao salvar skill.");
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
          placeholder="Ex: Revisão de contratos"
        />

        <TextField
          label="Descrição"
          fullWidth
          value={form.description}
          onChange={(e) => set("description", e.target.value)}
          placeholder="Uma linha explicando o que essa skill ensina o agente a fazer"
        />

        <Box>
          <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ minHeight: 36, mb: 1 }}>
            <Tab label="Markdown" value="edit" sx={{ minHeight: 36, py: 0.5 }} />
            <Tab label="Pré-visualização" value="preview" sx={{ minHeight: 36, py: 0.5 }} />
          </Tabs>

          {tab === "edit" ? (
            <TextField
              required
              fullWidth
              multiline
              minRows={10}
              maxRows={20}
              value={form.content}
              onChange={(e) => set("content", e.target.value)}
              placeholder="# Instruções da skill&#10;&#10;Descreva em markdown o que o agente deve saber/fazer..."
              slotProps={{ input: { sx: { fontFamily: "monospace", fontSize: 13 } } }}
            />
          ) : (
            <Paper variant="outlined" sx={{ p: 2, minHeight: 220, maxHeight: 420, overflowY: "auto" }}>
              {form.content ? (
                <Box sx={{ fontSize: 14, lineHeight: 1.6, "& pre": { overflowX: "auto" } }}>
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>{form.content}</ReactMarkdown>
                </Box>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Nada para pré-visualizar ainda.
                </Typography>
              )}
            </Paper>
          )}
        </Box>

        {error && <Alert severity="error">{error}</Alert>}

        <Stack direction="row" spacing={1.5} sx={{ justifyContent: "flex-end" }}>
          <Button onClick={onCancel} color="inherit">
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={submitting}>
            {submitting ? "Salvando..." : editing ? "Salvar alterações" : "Criar skill"}
          </Button>
        </Stack>
      </Stack>
    </Box>
  );
}
