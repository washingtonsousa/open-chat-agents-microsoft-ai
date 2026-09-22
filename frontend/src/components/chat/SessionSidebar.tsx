"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  Avatar,
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
  Tooltip,
  Typography,
} from "@mui/material";
import SmartToyIcon from "@mui/icons-material/SmartToy";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import StorageIcon from "@mui/icons-material/Storage";
import GroupIcon from "@mui/icons-material/Group";
import LogoutIcon from "@mui/icons-material/Logout";
import ChatBubbleOutlineIcon from "@mui/icons-material/ChatBubbleOutlineOutlined";
import DashboardIcon from "@mui/icons-material/DashboardOutlined";
import ExtensionIcon from "@mui/icons-material/Extension";
import ApiIcon from "@mui/icons-material/Api";
import { useCurrentUser } from "@/components/auth/AuthGuard";
import { authStorage } from "@/services/api";
import type { Agent, Session } from "@/types";

export const DRAWER_WIDTH = 288;

interface Props {
  sessions: Session[];
  agents: Agent[];
  activeSessionId: string | null;
  onSelect: (id: string) => void;
  onNewSession: () => void;
  onNewAgent: () => void;
  onEditAgent: (agent: Agent) => void;
  onDeleteSession: (id: string) => void;
  onDeleteAgent: (id: string) => void;
}

export function SessionSidebar({
  sessions,
  agents,
  activeSessionId,
  onSelect,
  onNewSession,
  onNewAgent,
  onEditAgent,
  onDeleteSession,
  onDeleteAgent,
}: Props) {
  const user = useCurrentUser();
  const router = useRouter();

  function handleLogout() {
    authStorage.clearToken();
    router.push("/login");
  }

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: DRAWER_WIDTH,
        flexShrink: 0,
        [`& .MuiDrawer-paper`]: {
          width: DRAWER_WIDTH,
          boxSizing: "border-box",
          bgcolor: "background.paper",
        },
      }}
    >
      <Toolbar sx={{ px: 2.5 }}>
        <Typography variant="h6" sx={{ fontWeight: 600, color: "primary.main" }}>
          Open Chat Agents
        </Typography>
      </Toolbar>
      <Divider />

      <Box sx={{ flex: 1, overflowY: "auto" }}>
        <List
          dense
          subheader={
            <ListSubheader component="div" sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", pr: 1 }}>
              Agentes
              <Tooltip title="Novo agente">
                <IconButton size="small" onClick={onNewAgent}>
                  <AddIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            </ListSubheader>
          }
        >
          {agents.length === 0 ? (
            <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>
              Nenhum agente criado.
            </Typography>
          ) : (
            agents.map((a) => (
              <ListItemButton
                key={a.id}
                sx={{ "&:hover .agent-actions": { opacity: 1 } }}
              >
                <ListItemAvatar sx={{ minWidth: 40 }}>
                  <Avatar sx={{ width: 30, height: 30, bgcolor: "primary.light" }}>
                    <SmartToyIcon fontSize="small" />
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary={a.name}
                  secondary={a.llm_model}
                  slotProps={{
                    primary: { noWrap: true, sx: { fontSize: 13, fontWeight: 500 } },
                    secondary: { noWrap: true, sx: { fontSize: 11 } },
                  }}
                />
                <Box className="agent-actions" sx={{ display: "flex", opacity: 0, transition: "opacity .15s" }}>
                  <IconButton size="small" onClick={() => onEditAgent(a)} title="Editar agente">
                    <EditIcon sx={{ fontSize: 15 }} />
                  </IconButton>
                  <IconButton size="small" onClick={() => onDeleteAgent(a.id)} title="Excluir agente" color="error">
                    <DeleteIcon sx={{ fontSize: 15 }} />
                  </IconButton>
                </Box>
              </ListItemButton>
            ))
          )}
        </List>

        <Divider sx={{ my: 1 }} />

        <List
          dense
          subheader={
            <ListSubheader component="div" sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", pr: 1 }}>
              Conversas
              <Tooltip title="Nova conversa">
                <IconButton size="small" onClick={onNewSession}>
                  <AddIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            </ListSubheader>
          }
        >
          {sessions.length === 0 ? (
            <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>
              Nenhuma conversa ainda.
            </Typography>
          ) : (
            sessions.map((s) => (
              <ListItemButton
                key={s.id}
                selected={s.id === activeSessionId}
                onClick={() => onSelect(s.id)}
                sx={{ "&:hover .session-actions": { opacity: 1 } }}
              >
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
                <IconButton
                  className="session-actions"
                  size="small"
                  color="error"
                  sx={{ opacity: 0, transition: "opacity .15s" }}
                  onClick={(e) => {
                    e.stopPropagation();
                    onDeleteSession(s.id);
                  }}
                >
                  <DeleteIcon sx={{ fontSize: 15 }} />
                </IconButton>
              </ListItemButton>
            ))
          )}
        </List>
      </Box>

      <Divider />
      <List dense>
        <ListItemButton component={Link} href="/">
          <ListItemAvatar sx={{ minWidth: 36 }}>
            <DashboardIcon fontSize="small" color="action" />
          </ListItemAvatar>
          <ListItemText primary="Painel" slotProps={{ primary: { sx: { fontSize: 13 } } }} />
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
        <ListItemButton onClick={handleLogout}>
          <ListItemAvatar sx={{ minWidth: 36 }}>
            <LogoutIcon fontSize="small" color="action" />
          </ListItemAvatar>
          <ListItemText
            primary={user?.username ?? "Sair"}
            slotProps={{ primary: { sx: { fontSize: 13 } } }}
          />
          {user?.is_admin && <Chip label="admin" size="small" color="secondary" sx={{ height: 18, fontSize: 10 }} />}
        </ListItemButton>
      </List>

      <Fab
        color="primary"
        size="medium"
        onClick={onNewSession}
        sx={{ position: "absolute", bottom: 88, right: 20 }}
        title="Nova conversa"
      >
        <AddIcon />
      </Fab>
    </Drawer>
  );
}
