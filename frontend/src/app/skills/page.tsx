"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  Alert,
  Box,
  Divider,
  Drawer,
  Fab,
  IconButton,
  List,
  ListItemAvatar,
  ListItemButton,
  ListItemSecondaryAction,
  ListItemText,
  Paper,
  Toolbar,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import AutoAwesomeIcon from "@mui/icons-material/AutoAwesome";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { AuthGuard } from "@/components/auth/AuthGuard";
import { SkillModal } from "@/components/skill/SkillModal";
import { skillApi } from "@/services/api";
import type { Skill } from "@/types";

const DRAWER_WIDTH = 288;

function SkillsContent() {
  const [skills, setSkills] = useState<Skill[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [editing, setEditing] = useState<Skill | undefined>(undefined);
  const [showModal, setShowModal] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    const res = await skillApi.list();
    setSkills(res.skills);
    setLoading(false);
  }

  useEffect(() => {
    load();
  }, []);

  function handleSaved(skill: Skill) {
    setSkills((prev) => {
      const exists = prev.some((s) => s.id === skill.id);
      return exists ? prev.map((s) => (s.id === skill.id ? skill : s)) : [skill, ...prev];
    });
    setActiveId(skill.id);
    setShowModal(false);
    setEditing(undefined);
  }

  async function handleDelete(id: string) {
    setError(null);
    try {
      await skillApi.delete(id);
      setSkills((prev) => prev.filter((s) => s.id !== id));
      if (activeId === id) setActiveId(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao excluir skill.");
    }
  }

  const active = skills.find((s) => s.id === activeId) ?? null;

  return (
    <Box sx={{ display: "flex", height: "100%" }}>
      <Drawer
        variant="permanent"
        sx={{
          width: DRAWER_WIDTH,
          flexShrink: 0,
          [`& .MuiDrawer-paper`]: { width: DRAWER_WIDTH, boxSizing: "border-box" },
        }}
      >
        <Toolbar sx={{ px: 2.5 }}>
          <IconButton component={Link} href="/" size="small" sx={{ mr: 1 }}>
            <ArrowBackIcon fontSize="small" />
          </IconButton>
          <Typography variant="body2" sx={{ fontWeight: 500 }}>
            Painel
          </Typography>
        </Toolbar>
        <Divider />

        <List
          dense
          sx={{ flex: 1, overflowY: "auto" }}
          subheader={
            <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", px: 2, py: 1 }}>
              <Typography variant="caption" sx={{ fontWeight: 600, color: "text.secondary" }}>
                SKILLS
              </Typography>
              <IconButton size="small" onClick={() => setShowModal(true)} title="Nova skill">
                <AddIcon fontSize="small" />
              </IconButton>
            </Box>
          }
        >
          {loading ? (
            <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>
              Carregando...
            </Typography>
          ) : skills.length === 0 ? (
            <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>
              Nenhuma skill criada ainda.
            </Typography>
          ) : (
            skills.map((skill) => (
              <ListItemButton key={skill.id} selected={skill.id === activeId} onClick={() => setActiveId(skill.id)}>
                <ListItemAvatar sx={{ minWidth: 36 }}>
                  <AutoAwesomeIcon fontSize="small" color="success" />
                </ListItemAvatar>
                <ListItemText
                  primary={skill.name}
                  secondary={skill.created_by?.username}
                  slotProps={{
                    primary: { noWrap: true, sx: { fontSize: 13, fontWeight: 500 } },
                    secondary: { noWrap: true, sx: { fontSize: 11 } },
                  }}
                />
                <ListItemSecondaryAction>
                  <IconButton
                    size="small"
                    edge="end"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDelete(skill.id);
                    }}
                  >
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </ListItemSecondaryAction>
              </ListItemButton>
            ))
          )}
        </List>

        <Fab
          color="primary"
          size="medium"
          onClick={() => setShowModal(true)}
          sx={{ position: "absolute", bottom: 20, right: 20 }}
          title="Nova skill"
        >
          <AddIcon />
        </Fab>
      </Drawer>

      <Box component="main" sx={{ flex: 1, display: "flex", flexDirection: "column", overflow: "auto", p: 4 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {active ? (
          <Paper variant="outlined" sx={{ p: 3, maxWidth: 800 }}>
            <Box sx={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", mb: 1 }}>
              <Box>
                <Typography variant="h6">{active.name}</Typography>
                {active.description && (
                  <Typography variant="body2" color="text.secondary">
                    {active.description}
                  </Typography>
                )}
              </Box>
              <IconButton size="small" onClick={() => setEditing(active)} title="Editar skill">
                <EditIcon fontSize="small" />
              </IconButton>
            </Box>
            <Divider sx={{ my: 2 }} />
            <Box sx={{ fontSize: 14, lineHeight: 1.6, "& pre": { overflowX: "auto" } }}>
              <ReactMarkdown remarkPlugins={[remarkGfm]}>{active.content}</ReactMarkdown>
            </Box>
          </Paper>
        ) : (
          <Paper variant="outlined" sx={{ p: 3, maxWidth: 640 }}>
            <Typography variant="h6" sx={{ mb: 1 }}>
              Skills
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Skills são instruções reutilizáveis em markdown que você pode anexar a qualquer agente —
              o conteúdo é injetado direto no prompt do agente. Crie pela tela ou peça pelo chat (o servidor
              MCP embutido &quot;Criador de skills&quot;). Selecione uma na lista ao lado.
            </Typography>
          </Paper>
        )}
      </Box>

      {(showModal || editing) && (
        <SkillModal
          onSaved={handleSaved}
          onClose={() => {
            setShowModal(false);
            setEditing(undefined);
          }}
          editing={editing}
        />
      )}
    </Box>
  );
}

export default function SkillsPage() {
  return (
    <AuthGuard>
      <SkillsContent />
    </AuthGuard>
  );
}
