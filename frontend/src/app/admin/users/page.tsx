"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  Alert,
  Avatar,
  Box,
  Button,
  Checkbox,
  Chip,
  Container,
  FormControlLabel,
  IconButton,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import PersonIcon from "@mui/icons-material/Person";
import { AuthGuard, useCurrentUser } from "@/components/auth/AuthGuard";
import { userApi } from "@/services/api";
import type { User, UserCreate } from "@/types";

const DEFAULTS: UserCreate = { username: "", password: "", is_admin: false };

function UsersContent() {
  const currentUser = useCurrentUser();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState<UserCreate>(DEFAULTS);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function load() {
    const res = await userApi.list();
    setUsers(res.users);
    setLoading(false);
  }

  useEffect(() => {
    load();
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const user = await userApi.create(form);
      setUsers((prev) => [...prev, user].sort((a, b) => a.username.localeCompare(b.username)));
      setForm(DEFAULTS);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao criar usuário.");
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDelete(id: string) {
    if (!confirm("Remover este usuário?")) return;
    await userApi.delete(id);
    setUsers((prev) => prev.filter((u) => u.id !== id));
  }

  if (!currentUser?.is_admin) {
    return (
      <Box sx={{ display: "flex", height: "100%", alignItems: "center", justifyContent: "center" }}>
        <Typography color="text.secondary" variant="body2">
          Apenas administradores podem acessar esta página.
        </Typography>
      </Box>
    );
  }

  return (
    <Container maxWidth="sm" sx={{ py: 5 }}>
      <Stack direction="row" spacing={1} sx={{ mb: 3, alignItems: "center" }}>
        <IconButton component={Link} href="/" size="small">
          <ArrowBackIcon fontSize="small" />
        </IconButton>
        <Typography variant="body2" color="text.secondary">
          Painel
        </Typography>
      </Stack>

      <Typography variant="h5" sx={{ mb: 3, fontWeight: 600 }}>
        Usuários
      </Typography>

      <Paper component="form" onSubmit={handleSubmit} elevation={2} sx={{ p: 3, mb: 4, borderRadius: 3 }}>
        <Typography variant="subtitle2" sx={{ mb: 2 }}>
          Novo usuário
        </Typography>
        <Stack spacing={2}>
          <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
            <TextField
              label="Usuário"
              required
              size="small"
              value={form.username}
              onChange={(e) => setForm((f) => ({ ...f, username: e.target.value }))}
            />
            <TextField
              label="Senha inicial"
              type="password"
              required
              size="small"
              slotProps={{ htmlInput: { minLength: 4 } }}
              value={form.password}
              onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
            />
          </Box>
          <FormControlLabel
            control={
              <Checkbox
                checked={form.is_admin}
                onChange={(e) => setForm((f) => ({ ...f, is_admin: e.target.checked }))}
              />
            }
            label="Administrador"
          />
          {error && <Alert severity="error">{error}</Alert>}
          <Box>
            <Button type="submit" variant="contained" startIcon={<AddIcon />} disabled={submitting}>
              {submitting ? "Criando..." : "Criar usuário"}
            </Button>
          </Box>
          <Typography variant="caption" color="text.secondary">
            O usuário será obrigado a trocar a senha no primeiro login.
          </Typography>
        </Stack>
      </Paper>

      <Typography variant="subtitle2" sx={{ mb: 1.5 }}>
        Todos os usuários
      </Typography>
      {loading ? (
        <Typography variant="body2" color="text.secondary">
          Carregando...
        </Typography>
      ) : (
        <List sx={{ p: 0 }}>
          {users.map((u) => (
            <Paper key={u.id} variant="outlined" sx={{ mb: 1 }}>
              <ListItem
                secondaryAction={
                  u.id !== currentUser.id && (
                    <IconButton edge="end" color="error" onClick={() => handleDelete(u.id)}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  )
                }
              >
                <ListItemAvatar>
                  <Avatar sx={{ width: 32, height: 32 }}>
                    <PersonIcon fontSize="small" />
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary={
                    <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {u.username}
                      </Typography>
                      {u.is_admin && <Chip label="admin" size="small" color="secondary" sx={{ height: 18, fontSize: 10 }} />}
                    </Stack>
                  }
                  secondary={u.must_change_password ? "precisa trocar senha" : undefined}
                />
              </ListItem>
            </Paper>
          ))}
        </List>
      )}
    </Container>
  );
}

export default function UsersPage() {
  return (
    <AuthGuard>
      <UsersContent />
    </AuthGuard>
  );
}
