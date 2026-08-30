"use client";

import { useState } from "react";
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { authApi } from "@/services/api";

interface Props {
  forced?: boolean;
  onDone: () => void;
  onClose?: () => void;
}

export function ChangePasswordModal({ forced = false, onDone, onClose }: Props) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      setError("As senhas não coincidem.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await authApi.changePassword(currentPassword, newPassword);
      onDone();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao trocar senha.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog open maxWidth="xs" fullWidth onClose={forced ? undefined : onClose}>
      <DialogTitle>Troque sua senha</DialogTitle>
      <DialogContent>
        {forced && (
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Esta é sua primeira vez entrando (ou um administrador redefiniu sua senha). Escolha uma nova senha
            para continuar.
          </Typography>
        )}
        <Stack component="form" id="change-password-form" onSubmit={handleSubmit} spacing={2.5} sx={{ pt: 0.5 }}>
          <TextField
            label="Senha atual"
            type="password"
            required
            fullWidth
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
          />
          <TextField
            label="Nova senha"
            type="password"
            required
            fullWidth
            slotProps={{ htmlInput: { minLength: 4 } }}
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
          />
          <TextField
            label="Confirmar nova senha"
            type="password"
            required
            fullWidth
            slotProps={{ htmlInput: { minLength: 4 } }}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
          />
          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        {!forced && onClose && (
          <Button onClick={onClose} color="inherit">
            Cancelar
          </Button>
        )}
        <Button type="submit" form="change-password-form" variant="contained" disabled={submitting}>
          {submitting ? "Salvando..." : "Salvar"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
