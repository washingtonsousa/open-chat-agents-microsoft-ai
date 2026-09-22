"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  Alert,
  Box,
  Chip,
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
import ExtensionIcon from "@mui/icons-material/Extension";
import { AuthGuard } from "@/components/auth/AuthGuard";
import { McpServerModal } from "@/components/mcp-server/McpServerModal";
import { mcpServerApi } from "@/services/api";
import type { McpServer } from "@/types";

const DRAWER_WIDTH = 288;

const AUTH_LABEL: Record<McpServer["auth_type"], string> = {
  none: "sem autenticação",
  "bearer-token": "Bearer token",
  header: "header customizado",
};

function McpServersContent() {
  const [servers, setServers] = useState<McpServer[]>([]);
  const [editing, setEditing] = useState<McpServer | undefined>(undefined);
  const [showModal, setShowModal] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    const res = await mcpServerApi.list();
    setServers(res.mcp_servers);
    setLoading(false);
  }

  useEffect(() => {
    load();
  }, []);

  function handleSaved(server: McpServer) {
    setServers((prev) => {
      const exists = prev.some((s) => s.id === server.id);
      return exists ? prev.map((s) => (s.id === server.id ? server : s)) : [server, ...prev];
    });
    setShowModal(false);
    setEditing(undefined);
  }

  async function handleDelete(id: string) {
    setError(null);
    try {
      await mcpServerApi.delete(id);
      setServers((prev) => prev.filter((s) => s.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao excluir servidor MCP.");
    }
  }

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
                SERVIDORES MCP
              </Typography>
              <IconButton size="small" onClick={() => setShowModal(true)} title="Novo servidor">
                <AddIcon fontSize="small" />
              </IconButton>
            </Box>
          }
        >
          {loading ? (
            <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>
              Carregando...
            </Typography>
          ) : servers.length === 0 ? (
            <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>
              Nenhum servidor cadastrado ainda.
            </Typography>
          ) : (
            servers.map((server) => (
              <ListItemButton key={server.id} onClick={() => setEditing(server)}>
                <ListItemAvatar sx={{ minWidth: 36 }}>
                  <ExtensionIcon fontSize="small" color="secondary" />
                </ListItemAvatar>
                <ListItemText
                  primary={server.name}
                  secondary={AUTH_LABEL[server.auth_type]}
                  slotProps={{ primary: { noWrap: true, sx: { fontSize: 13, fontWeight: 500 } } }}
                />
                <ListItemSecondaryAction>
                  <IconButton
                    size="small"
                    edge="end"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDelete(server.id);
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
          title="Novo servidor MCP"
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
        <Paper variant="outlined" sx={{ p: 3, maxWidth: 640 }}>
          <Typography variant="h6" sx={{ mb: 1 }}>
            Servidores MCP
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Cadastre servidores MCP (Model Context Protocol) remotos, com ou sem autenticação, e anexe-os a
            qualquer agente para dar acesso a ferramentas externas durante a conversa. Clique num servidor na
            lista ao lado para editar.
          </Typography>
        </Paper>
      </Box>

      {(showModal || editing) && (
        <McpServerModal
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

export default function McpServersPage() {
  return (
    <AuthGuard>
      <McpServersContent />
    </AuthGuard>
  );
}
