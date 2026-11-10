import { useState, useEffect, useCallback } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Button,
  Typography,
  Chip,
  Alert,
  CircularProgress,
  Box,
} from '@mui/material';
import type { MyHoldResponse } from '../types';
import { getMyHolds, confirmHold, cancelHold } from '../api';

interface Props {
  studentId: string;
  refreshTrigger: number;
  onActionComplete: () => void;
}

export default function MyHolds({ studentId, refreshTrigger, onActionComplete }: Props) {
  const [holds, setHolds] = useState<MyHoldResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [actionHoldId, setActionHoldId] = useState<string | null>(null);
  const [actionError, setActionError] = useState('');

  const fetchHolds = useCallback(async () => {
    if (!studentId) {
      setHolds([]);
      return;
    }
    setLoading(true);
    try {
      const data = await getMyHolds(studentId);
      setHolds(data);
      setError('');
    } catch (err: any) {
      setError(err?.response?.data?.error || err?.message || 'Failed to load holds.');
    } finally {
      setLoading(false);
    }
  }, [studentId]);

  useEffect(() => {
    fetchHolds();
  }, [fetchHolds, refreshTrigger]);

  // Timer: update countdown and auto-expire HELD holds every second
  useEffect(() => {
    const interval = setInterval(() => {
      setHolds((prev) =>
        prev.map((h) => {
          if (h.status === 'HELD' && new Date(h.expiresAt).getTime() <= Date.now()) {
            return { ...h, status: 'EXPIRED' };
          }
          return h;
        })
      );
    }, 1000);
    return () => clearInterval(interval);
  }, []);

  // Also refresh from server periodically
  useEffect(() => {
    if (!studentId) return;
    const interval = setInterval(fetchHolds, 10000);
    return () => clearInterval(interval);
  }, [fetchHolds, studentId]);

  const handleConfirm = async (holdId: string) => {
    setActionHoldId(holdId);
    setActionError('');
    try {
      await confirmHold(holdId, studentId);
      await fetchHolds();
      onActionComplete();
    } catch (err: any) {
      const msg = err?.response?.data?.error || err?.message || 'Failed to confirm.';
      setActionError(msg);
    } finally {
      setActionHoldId(null);
    }
  };

  const handleCancel = async (holdId: string) => {
    setActionHoldId(holdId);
    setActionError('');
    try {
      await cancelHold(holdId, studentId);
      await fetchHolds();
      onActionComplete();
    } catch (err: any) {
      const msg = err?.response?.data?.error || err?.message || 'Failed to cancel.';
      setActionError(msg);
    } finally {
      setActionHoldId(null);
    }
  };

  const getStatusColor = (status: string): 'warning' | 'success' | 'error' | 'default' => {
    switch (status) {
      case 'HELD':
        return 'warning';
      case 'CONFIRMED':
        return 'success';
      case 'EXPIRED':
      case 'CANCELLED':
        return 'error';
      default:
        return 'default';
    }
  };

  if (!studentId) {
    return (
      <Typography color="text.secondary" sx={{ p: 2 }}>
        Enter your Birth Registration Number above to see your applications.
      </Typography>
    );
  }

  if (loading && holds.length === 0) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      {actionError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setActionError('')}>
          {actionError}
        </Alert>
      )}

      {holds.length === 0 ? (
        <Typography color="text.secondary" sx={{ p: 2 }}>
          No applications found for this student.
        </Typography>
      ) : (
        <TableContainer component={Paper} variant="outlined">
          <Table>
            <TableHead>
              <TableRow sx={{ backgroundColor: 'grey.100' }}>
                <TableCell><strong>Class</strong></TableCell>
                <TableCell align="center"><strong>Status</strong></TableCell>
                <TableCell align="center"><strong>Time Remaining</strong></TableCell>
                <TableCell align="center"><strong>Actions</strong></TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {holds.map((hold) => (
                <TableRow key={hold.holdId} hover>
                  <TableCell>{hold.className}</TableCell>
                  <TableCell align="center">
                    <Chip
                      label={hold.status}
                      size="small"
                      color={getStatusColor(hold.status)}
                    />
                  </TableCell>
                  <TableCell align="center">
                    {hold.status === 'HELD' ? (
                      <CountdownTimer expiresAt={hold.expiresAt} />
                    ) : (
                      '-'
                    )}
                  </TableCell>
                  <TableCell align="center">
                    {hold.status === 'HELD' && (
                      <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center' }}>
                        <Button
                          variant="contained"
                          color="success"
                          size="small"
                          disabled={actionHoldId === hold.holdId || new Date(hold.expiresAt).getTime() <= Date.now()}
                          onClick={() => handleConfirm(hold.holdId)}
                        >
                          {actionHoldId === hold.holdId ? 'Processing...' : 'Pay & Confirm'}
                        </Button>
                        <Button
                          variant="outlined"
                          color="error"
                          size="small"
                          disabled={actionHoldId === hold.holdId}
                          onClick={() => handleCancel(hold.holdId)}
                        >
                          Cancel
                        </Button>
                      </Box>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </>
  );
}

// Countdown timer component that updates every second
function CountdownTimer({ expiresAt }: { expiresAt: string }) {
  const [timeLeft, setTimeLeft] = useState('');

  useEffect(() => {
    const update = () => {
      const diff = new Date(expiresAt).getTime() - Date.now();
      if (diff <= 0) {
        setTimeLeft('Expired');
        return;
      }
      const minutes = Math.floor(diff / 60000);
      const seconds = Math.floor((diff % 60000) / 1000);
      setTimeLeft(`${minutes}m ${seconds}s`);
    };

    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, [expiresAt]);

  return (
    <Typography
      variant="body2"
      color={timeLeft === 'Expired' ? 'error' : 'text.primary'}
      sx={{ fontWeight: 'bold' }}
    >
      {timeLeft}
    </Typography>
  );
}
