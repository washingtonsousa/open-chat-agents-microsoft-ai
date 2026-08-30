"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  Box,
  Chip,
  Divider,
  Drawer,
  Fab,
  IconButton,
  List,
  ListItemAvatar,
  ListItemButton,
  ListItemText,
  ListSubheader,
  Toolbar,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import AddIcon from "@mui/icons-material/Add";
import StorageIcon from "@mui/icons-material/Storage";
import { AuthGuard } from "@/components/auth/AuthGuard";
import { KnowledgeBaseModal } from "@/components/knowledge-base/KnowledgeBaseModal";
import { KbDetailPanel } from "@/components/knowledge-base/KbDetailPanel";
import { knowledgeBaseApi } from "@/services/api";
import type { KnowledgeBase } from "@/types";

const DRAWER_WIDTH = 288;

const STATUS_COLOR: Record<KnowledgeBase["status"], "default" | "info" | "success" | "error"> = {
  empty: "default",
  processing: "info",
  ready: "success",
  failed: "error",
};

const STATUS_LABEL: Record<KnowledgeBase["status"], string> = {
  empty: "vazia",
  processing: "processando",
  ready: "pronta",
  failed: "com falhas",
};

function KnowledgeBasesContent() {
  const [kbs, setKbs] = useState<KnowledgeBase[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [loading, setLoading] = useState(true);

  async function load() {
    const res = await knowledgeBaseApi.list();
    setKbs(res.knowledge_bases);
    setLoading(false);
  }

  useEffect(() => {
    load();
  }, []);

  function handleCreated(kb: KnowledgeBase) {
    setKbs((prev) => [kb, ...prev]);
    setActiveId(kb.id);
    setShowModal(false);
  }

  function handleDeleted() {
    setKbs((prev) => prev.filter((k) => k.id !== activeId));
    setActiveId(null);
  }

  const active = kbs.find((k) => k.id === activeId) ?? null;

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
            Voltar ao chat
          </Typography>
        </Toolbar>
        <Divider />

        <List
          dense
          sx={{ flex: 1, overflowY: "auto" }}
          subheader={
            <ListSubheader component="div" sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", pr: 1 }}>
              Bases de conhecimento
              <IconButton size="small" onClick={() => setShowModal(true)} title="Nova KB">
                <AddIcon fontSize="small" />
              </IconButton>
            </ListSubheader>
          }
        >
          {loading ? (
            <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>
              Carregando...
            </Typography>
          ) : kbs.length === 0 ? (
            <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>
              Nenhuma base criada ainda.
            </Typography>
          ) : (
            kbs.map((kb) => (
              <ListItemButton key={kb.id} selected={kb.id === activeId} onClick={() => setActiveId(kb.id)}>
                <ListItemAvatar sx={{ minWidth: 36 }}>
                  <StorageIcon fontSize="small" color="primary" />
                </ListItemAvatar>
                <ListItemText primary={kb.name} slotProps={{ primary: { noWrap: true, sx: { fontSize: 13, fontWeight: 500 } } }} />
                <Chip size="small" label={STATUS_LABEL[kb.status]} color={STATUS_COLOR[kb.status]} sx={{ height: 20, fontSize: 10 }} />
              </ListItemButton>
            ))
          )}
        </List>

        <Fab
          color="primary"
          size="medium"
          onClick={() => setShowModal(true)}
          sx={{ position: "absolute", bottom: 20, right: 20 }}
          title="Nova base de conhecimento"
        >
          <AddIcon />
        </Fab>
      </Drawer>

      <Box component="main" sx={{ flex: 1, display: "flex", flexDirection: "column", overflow: "hidden" }}>
        {active ? (
          <KbDetailPanel kb={active} onDeleted={handleDeleted} />
        ) : (
          <Box sx={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center" }}>
            <Box>
              <Typography variant="h6" color="text.secondary">
                Nenhuma base selecionada
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Crie ou selecione uma base de conhecimento
              </Typography>
            </Box>
          </Box>
        )}
      </Box>

      {showModal && <KnowledgeBaseModal onCreated={handleCreated} onClose={() => setShowModal(false)} />}
    </Box>
  );
}

export default function KnowledgeBasesPage() {
  return (
    <AuthGuard>
      <KnowledgeBasesContent />
    </AuthGuard>
  );
}
