"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  Alert,
  Avatar,
  Box,
  Button,
  Chip,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Paper,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import ApiIcon from "@mui/icons-material/Api";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import CheckIcon from "@mui/icons-material/Check";
import { AuthGuard, useCurrentUser } from "@/components/auth/AuthGuard";
import { consumerApplicationApi } from "@/services/api";
import type { ConsumerApplication, ConsumerApplicationCreated } from "@/types";

function CopyField({ label, value }: { label: string; value: string }) {
  const [copied, setCopied] = useState(false);

  function handleCopy() {
    navigator.clipboard.writeText(value);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <TextField
      label={label}
      value={value}
      fullWidth
      size="small"
      slotProps={{
        input: {
          readOnly: true,
          sx: { fontFamily: "monospace", fontSize: 13 },
          endAdornment: (
            <Tooltip title={copied ? "Copiado!" : "Copiar"}>
              <IconButton size="small" onClick={handleCopy}>
                {copied ? <CheckIcon fontSize="small" /> : <ContentCopyIcon fontSize="small" />}
              </IconButton>
            </Tooltip>
          ),
        },
      }}
    />
  );
}

function ConsumerApplicationsContent() {
  const currentUser = useCurrentUser();
  const [apps, setApps] = useState<ConsumerApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [created, setCreated] = useState<ConsumerApplicationCreated | null>(null);

  async function load() {
    const res = await consumerApplicationApi.list();
    setApps(res.consumer_applications);
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
      const app = await consumerApplicationApi.create({ name });
      setName("");
      setCreated(app);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao criar aplicação consumidora.");
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDelete(id: string) {
    if (!confirm("Revogar o acesso desta aplicação?")) return;
    await consumerApplicationApi.delete(id);
    setApps((prev) => prev.filter((a) => a.id !== id));
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

      <Typography variant="h5" sx={{ mb: 1, fontWeight: 600 }}>
        Aplicações consumidoras
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Cada aplicação recebe um <code>client_id</code>/<code>client_secret</code> próprios para trocar por um
        token e consumir os agentes remotamente via <code>POST /api/v1/auth/token</code>.
      </Typography>

      <Paper component="form" onSubmit={handleSubmit} elevation={2} sx={{ p: 3, mb: 4, borderRadius: 3 }}>
        <Typography variant="subtitle2" sx={{ mb: 2 }}>
          Nova aplicação
        </Typography>
        <Stack spacing={2}>
          <TextField
            label="Nome"
            required
            size="small"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Ex: Integração com o CRM"
          />
          {error && <Alert severity="error">{error}</Alert>}
          <Box>
            <Button type="submit" variant="contained" startIcon={<AddIcon />} disabled={submitting}>
              {submitting ? "Criando..." : "Criar aplicação"}
            </Button>
          </Box>
        </Stack>
      </Paper>

      <Typography variant="subtitle2" sx={{ mb: 1.5 }}>
        Todas as aplicações
      </Typography>
      {loading ? (
        <Typography variant="body2" color="text.secondary">
          Carregando...
        </Typography>
      ) : (
        <List sx={{ p: 0 }}>
          {apps.map((a) => (
            <Paper key={a.id} variant="outlined" sx={{ mb: 1 }}>
              <ListItem
                secondaryAction={
                  <IconButton edge="end" color="error" onClick={() => handleDelete(a.id)}>
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                }
              >
                <ListItemAvatar>
                  <Avatar sx={{ width: 32, height: 32 }}>
                    <ApiIcon fontSize="small" />
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary={
                    <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {a.name}
                      </Typography>
                      {!a.is_active && <Chip label="inativa" size="small" color="default" sx={{ height: 18, fontSize: 10 }} />}
                    </Stack>
                  }
                  secondary={a.client_id}
                  slotProps={{ secondary: { sx: { fontFamily: "monospace", fontSize: 11 } } }}
                />
              </ListItem>
            </Paper>
          ))}
        </List>
      )}

      <Dialog open={created !== null} onClose={() => setCreated(null)} fullWidth maxWidth="sm">
        <DialogTitle>Aplicação criada</DialogTitle>
        <DialogContent>
          <Alert severity="warning" sx={{ mb: 2 }}>
            Guarde o <strong>client secret</strong> agora — ele não será mostrado novamente.
          </Alert>
          {created && (
            <Stack spacing={2}>
              <CopyField label="Client ID" value={created.client_id} />
              <CopyField label="Client Secret" value={created.client_secret} />
            </Stack>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setCreated(null)} variant="contained">
            Já guardei, fechar
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}

export default function ConsumerApplicationsPage() {
  return (
    <AuthGuard>
      <ConsumerApplicationsContent />
    </AuthGuard>
  );
}
