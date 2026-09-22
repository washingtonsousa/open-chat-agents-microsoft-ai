"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  Avatar,
  Box,
  Button,
  Chip,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemAvatar,
  ListItemButton,
  ListItemText,
  Paper,
  Stack,
  Toolbar,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import SmartToyIcon from "@mui/icons-material/SmartToy";
import StorageIcon from "@mui/icons-material/Storage";
import ExtensionIcon from "@mui/icons-material/Extension";
import ChatBubbleOutlineIcon from "@mui/icons-material/ChatBubbleOutlineOutlined";
import GroupIcon from "@mui/icons-material/Group";
import ApiIcon from "@mui/icons-material/Api";
import LogoutIcon from "@mui/icons-material/Logout";
import DashboardIcon from "@mui/icons-material/DashboardOutlined";
import { AuthGuard, useCurrentUser } from "@/components/auth/AuthGuard";
import { AgentModal } from "@/components/agent/AgentModal";
import { KnowledgeBaseModal } from "@/components/knowledge-base/KnowledgeBaseModal";
import { McpServerModal } from "@/components/mcp-server/McpServerModal";
import { agentApi, authStorage, knowledgeBaseApi, mcpServerApi, sessionApi } from "@/services/api";
import type { Agent, KnowledgeBase, McpServer, Session } from "@/types";

const DRAWER_WIDTH = 288;

interface Insight {
  label: string;
  count: number;
  icon: React.ReactNode;
  href: string;
}

function DashboardContent() {
  const user = useCurrentUser();
  const router = useRouter();

  const [agents, setAgents] = useState<Agent[]>([]);
  const [kbs, setKbs] = useState<KnowledgeBase[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [mcpServers, setMcpServers] = useState<McpServer[]>([]);
  const [loading, setLoading] = useState(true);

  const [showAgentModal, setShowAgentModal] = useState(false);
  const [showKbModal, setShowKbModal] = useState(false);
  const [showMcpModal, setShowMcpModal] = useState(false);

  async function load() {
    const [a, k, s, m] = await Promise.all([
      agentApi.list(),
      knowledgeBaseApi.list(),
      sessionApi.list(),
      mcpServerApi.list(),
    ]);
    setAgents(a.agents);
    setKbs(k.knowledge_bases);
    setSessions(s.sessions);
    setMcpServers(m.mcp_servers);
    setLoading(false);
  }

  useEffect(() => {
    load();
  }, []);

  function handleLogout() {
    authStorage.clearToken();
    router.push("/login");
  }

  const insights: Insight[] = [
    { label: "Agentes", count: agents.length, icon: <SmartToyIcon />, href: "/chat" },
    { label: "Bases de conhecimento", count: kbs.length, icon: <StorageIcon />, href: "/knowledge-bases" },
    { label: "Conversas", count: sessions.length, icon: <ChatBubbleOutlineIcon />, href: "/chat" },
    { label: "Servidores MCP", count: mcpServers.length, icon: <ExtensionIcon />, href: "/mcp-servers" },
  ];

  return (
    <Box sx={{ display: "flex", height: "100%" }}>
      <Drawer
        variant="permanent"
        sx={{
          width: DRAWER_WIDTH,
          flexShrink: 0,
          [`& .MuiDrawer-paper`]: { width: DRAWER_WIDTH, boxSizing: "border-box", bgcolor: "background.paper" },
        }}
      >
        <Toolbar sx={{ px: 2.5 }}>
          <Typography variant="h6" sx={{ fontWeight: 600, color: "primary.main" }}>
            Open Chat Agents
          </Typography>
        </Toolbar>
        <Divider />

        <List dense sx={{ flex: 1 }}>
          <ListItemButton selected>
            <ListItemAvatar sx={{ minWidth: 36 }}>
              <DashboardIcon fontSize="small" color="primary" />
            </ListItemAvatar>
            <ListItemText primary="Painel" slotProps={{ primary: { sx: { fontSize: 13, fontWeight: 500 } } }} />
          </ListItemButton>
          <ListItemButton component={Link} href="/chat">
            <ListItemAvatar sx={{ minWidth: 36 }}>
              <ChatBubbleOutlineIcon fontSize="small" color="action" />
            </ListItemAvatar>
            <ListItemText primary="Chat" slotProps={{ primary: { sx: { fontSize: 13 } } }} />
          </ListItemButton>
          <ListItemButton component={Link} href="/knowledge-bases">
            <ListItemAvatar sx={{ minWidth: 36 }}>
              <StorageIcon fontSize="small" color="action" />
            </ListItemAvatar>
            <ListItemText primary="Bases de conhecimento" slotProps={{ primary: { sx: { fontSize: 13 } } }} />
          </ListItemButton>
          <ListItemButton component={Link} href="/mcp-servers">
            <ListItemAvatar sx={{ minWidth: 36 }}>
              <ExtensionIcon fontSize="small" color="action" />
            </ListItemAvatar>
            <ListItemText primary="Servidores MCP" slotProps={{ primary: { sx: { fontSize: 13 } } }} />
          </ListItemButton>
          {user?.is_admin && (
            <ListItemButton component={Link} href="/admin/users">
              <ListItemAvatar sx={{ minWidth: 36 }}>
                <GroupIcon fontSize="small" color="action" />
              </ListItemAvatar>
              <ListItemText primary="Usuários" slotProps={{ primary: { sx: { fontSize: 13 } } }} />
            </ListItemButton>
          )}
          {user?.is_admin && (
            <ListItemButton component={Link} href="/admin/consumer-applications">
              <ListItemAvatar sx={{ minWidth: 36 }}>
                <ApiIcon fontSize="small" color="action" />
              </ListItemAvatar>
              <ListItemText primary="Aplicações consumidoras" slotProps={{ primary: { sx: { fontSize: 13 } } }} />
            </ListItemButton>
          )}
        </List>

        <Divider />
        <List dense>
          <ListItemButton onClick={handleLogout}>
            <ListItemAvatar sx={{ minWidth: 36 }}>
              <LogoutIcon fontSize="small" color="action" />
            </ListItemAvatar>
            <ListItemText primary={user?.username ?? "Sair"} slotProps={{ primary: { sx: { fontSize: 13 } } }} />
            {user?.is_admin && <Chip label="admin" size="small" color="secondary" sx={{ height: 18, fontSize: 10 }} />}
          </ListItemButton>
        </List>
      </Drawer>

      <Box component="main" sx={{ flex: 1, overflow: "auto", p: 4 }}>
        <Typography variant="h5" sx={{ fontWeight: 600, mb: 0.5 }}>
          Bem-vindo(a), {user?.username}!
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
          Um resumo da sua plataforma e atalhos para o que você usa com mais frequência.
        </Typography>

        <Box sx={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 2, mb: 4 }}>
          {insights.map((item) => (
            <Paper
              key={item.label}
              component={Link}
              href={item.href}
              elevation={1}
              sx={{
                p: 2.5,
                display: "flex",
                alignItems: "center",
                gap: 1.5,
                textDecoration: "none",
                color: "inherit",
                transition: "box-shadow .15s",
                "&:hover": { boxShadow: 4 },
              }}
            >
              <Avatar sx={{ bgcolor: "primary.light" }}>{item.icon}</Avatar>
              <Box>
                <Typography variant="h5" sx={{ fontWeight: 600, lineHeight: 1 }}>
                  {loading ? "—" : item.count}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {item.label}
                </Typography>
              </Box>
            </Paper>
          ))}
        </Box>

        <Typography variant="subtitle2" sx={{ mb: 1.5 }}>
          Ações rápidas
        </Typography>
        <Stack direction="row" spacing={1.5} sx={{ mb: 4, flexWrap: "wrap", gap: 1.5 }}>
          <Button variant="contained" startIcon={<AddIcon />} component={Link} href="/chat">
            Nova conversa
          </Button>
          <Button variant="outlined" startIcon={<SmartToyIcon />} onClick={() => setShowAgentModal(true)}>
            Novo agente
          </Button>
          <Button variant="outlined" startIcon={<StorageIcon />} onClick={() => setShowKbModal(true)}>
            Nova base de conhecimento
          </Button>
          <Button variant="outlined" startIcon={<ExtensionIcon />} onClick={() => setShowMcpModal(true)}>
            Novo servidor MCP
          </Button>
        </Stack>

        <Typography variant="subtitle2" sx={{ mb: 1.5 }}>
          Conversas recentes
        </Typography>
        {loading ? (
          <Typography variant="body2" color="text.secondary">
            Carregando...
          </Typography>
        ) : sessions.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            Nenhuma conversa ainda — comece uma nova acima.
          </Typography>
        ) : (
          <Paper variant="outlined" sx={{ maxWidth: 560 }}>
            <List dense sx={{ p: 0 }}>
              {sessions.slice(0, 5).map((s) => (
                <ListItemButton key={s.id} component={Link} href={`/chat?session=${s.id}`}>
                  <ListItemAvatar sx={{ minWidth: 40 }}>
                    <Avatar sx={{ width: 30, height: 30 }}>
                      <ChatBubbleOutlineIcon fontSize="small" />
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={s.title}
                    secondary={s.agent?.name}
                    slotProps={{
                      primary: { noWrap: true, sx: { fontSize: 13, fontWeight: 500 } },
                      secondary: { noWrap: true, sx: { fontSize: 11 } },
                    }}
                  />
                </ListItemButton>
              ))}
            </List>
          </Paper>
        )}
      </Box>

      {showAgentModal && (
        <AgentModal
          onSaved={() => {
            setShowAgentModal(false);
            load();
          }}
          onClose={() => setShowAgentModal(false)}
        />
      )}
      {showKbModal && (
        <KnowledgeBaseModal
          onCreated={() => {
            setShowKbModal(false);
            load();
          }}
          onClose={() => setShowKbModal(false)}
        />
      )}
      {showMcpModal && (
        <McpServerModal
          onSaved={() => {
            setShowMcpModal(false);
            load();
          }}
          onClose={() => setShowMcpModal(false)}
        />
      )}
    </Box>
  );
}

export default function DashboardPage() {
  return (
    <AuthGuard>
      <DashboardContent />
    </AuthGuard>
  );
}
